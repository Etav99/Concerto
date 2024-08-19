using Concerto.Server.Data.DatabaseContext;
using Concerto.Server.Data.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Immutable;

namespace Concerto.Server.Services;

public class WorkspaceService
{
	private readonly ConcertoDbContext _context;
	private readonly ILogger<WorkspaceService> _logger;
	private readonly StorageService _storageService;

	public WorkspaceService(ILogger<WorkspaceService> logger, ConcertoDbContext context, StorageService storageService)
	{
		_logger = logger;
		_context = context;
		_storageService = storageService;
	}

	// Create
	public async Task<long> CreateWorkspace(Dto.CreateWorkspaceRequest request, Guid userId)
	{
		// Create workspace
		var workspace = new Workspace
		{
			Name = request.Name,
			CreatedDate = DateTime.UtcNow,
			WorkspaceUsers = new [] { new WorkspaceUser(userId, WorkspaceUserRole.Admin) }
		};

		await using var transaction = _context.Database.BeginTransaction();

		await _context.Workspaces.AddAsync(workspace);
		await _context.SaveChangesAsync();

		var rootFolder = Folder.NewRoot(workspace.Id);
		var sessionsFolder = Folder.NewSessionsFolder(workspace.Id);

		await _context.Folders.AddAsync(rootFolder);
		await _context.Folders.AddAsync(sessionsFolder);
		await _context.SaveChangesAsync();

		workspace.RootFolderId = rootFolder.Id;
		workspace.SessionsFolderId = sessionsFolder.Id;

		await _context.SaveChangesAsync();

		await transaction.CommitAsync();

		return workspace.Id;
	}

	// Read
	public async Task<bool> IsUserWorkspaceMember(Guid userId, long workspaceId)
	{
		var workspaceUser = await _context.WorkspaceUsers.FindAsync(workspaceId, userId);
		return workspaceUser != null;
	}

	public async Task<Dto.Workspace?> GetWorkspace(long workspaceId, Guid userId, bool isAdmin = false)
	{
		var workspace = await _context.Workspaces.FindAsync(workspaceId);
		return workspace?.ToViewModel(isAdmin || await CanManageWorkspace(workspaceId, userId));
	}

	public async Task<Dto.WorkspaceSettings?> GetWorkspaceSettings(long workspaceId, Guid userId, bool isAdmin = false)
	{
		var workspace = await _context.Workspaces.FindAsync(workspaceId);
		if (workspace == null)
			return null;

		WorkspaceUserRole? workspaceRole;
		if (isAdmin)
		{
			workspaceRole = WorkspaceUserRole.Admin;
		}
		else
		{
			workspaceRole = (await _context.WorkspaceUsers.FindAsync(workspaceId, userId))?.Role;
			if (workspaceRole == null)
				return null;
		}

		await _context.Entry(workspace).Collection(c => c.WorkspaceUsers).LoadAsync();
		return workspace.ToSettingsViewModel(userId, workspaceRole.Value, workspaceRole == WorkspaceUserRole.Admin);
	}

	internal async Task<IEnumerable<Dto.User>> GetWorkspaceUsers(long workspaceId)
	{
		return await _context.WorkspaceUsers
			.Where(cu => cu.WorkspaceId == workspaceId)
			.Include(cu => cu.User)
			.Select(cu => cu.User.ToViewModel())
			.ToListAsync();
	}

	public async Task<IEnumerable<Dto.WorkspaceListItem>> GetUserWorkspacesList(Guid userId)
	{
		return await _context.WorkspaceUsers
			.Where(cu => cu.UserId == userId)
			.Include(cu => cu.Workspace)
			.Select(cu => cu.Workspace.ToWorkspaceListItem())
			.ToListAsync();
	}

	public async Task<IEnumerable<Dto.WorkspaceListItem>> GetAllWorkspaces()
	{
		return await _context.Workspaces
			.Select(r => r.ToWorkspaceListItem())
			.ToListAsync();
	}

	// Update
	internal async Task<bool> CanManageWorkspace(long workspaceId, Guid userId)
	{
		var workspaceRole = (await _context.WorkspaceUsers.FindAsync(workspaceId, userId))?.Role;
		return workspaceRole == WorkspaceUserRole.Admin;
	}

	internal async Task<bool> CanManageWorkspaceSessions(long workspaceId, Guid userId)
	{
		var workspaceRole = (await _context.WorkspaceUsers.FindAsync(workspaceId, userId))?.Role;
		return workspaceRole is WorkspaceUserRole.Admin or WorkspaceUserRole.Supervisor;
	}

	public async Task<bool> UpdateWorkspace(Dto.UpdateWorkspaceRequest request, Guid userId)
	{
		var workspace = await _context.Workspaces.FindAsync(request.WorkspaceId);
		if (workspace == null) return false;

		var updatingUser = await _context.WorkspaceUsers.FindAsync(request.WorkspaceId, userId);

		await _context.Entry(workspace).Collection(c => c.WorkspaceUsers).LoadAsync();

		var newWorkspaceUsers = request.Members.Select(member => new WorkspaceUser
		{
			WorkspaceId = workspace.Id,
			UserId = member.UserId,
			Role = member.Role.ToEntity()
		}
			)
			.ToList();

		if (updatingUser is not null) newWorkspaceUsers.Add(updatingUser);

		var deletedUsersIds = workspace.WorkspaceUsers.Select(cu => cu.UserId).Except(newWorkspaceUsers.Select(ncu => ncu.UserId)).ToHashSet();
		var deletedUserFolderPermissionsQuery
			= _context.UserFolderPermissions.Where(ufp => ufp.Folder.WorkspaceId == workspace.Id && deletedUsersIds.Contains(ufp.UserId));
		_context.RemoveRange(deletedUserFolderPermissionsQuery);

		workspace.Name = request.Name;
		workspace.Description = request.Description;
		workspace.WorkspaceUsers = newWorkspaceUsers;

		await _context.SaveChangesAsync();
		return true;
	}

	// Delete
	internal async Task<bool> CanDeleteWorkspace(long workspaceId, Guid userId)
	{
		var workspaceRole = (await _context.WorkspaceUsers.FindAsync(workspaceId, userId))?.Role;
		return workspaceRole == WorkspaceUserRole.Admin;
	}

	internal async Task<bool> DeleteWorkspace(long workspaceId)
	{
		var workspace = await _context.Workspaces.FindAsync(workspaceId);
		if (workspace == null) return false;

		await _context.Entry(workspace).Reference(c => c.RootFolder).LoadAsync();
		await _context.Entry(workspace).Collection(c => c.Sessions).LoadAsync();
		await _context.Entry(workspace).Reference(c => c.SessionsFolder).LoadAsync();

		await using var transaction = _context.Database.BeginTransaction();

		_context.RemoveRange(workspace.Sessions);
		await _context.SaveChangesAsync();

		if (workspace.RootFolder != null)
			await _storageService.DeleteFolder(workspace.RootFolder.Id);

		if (workspace.SessionsFolder != null)
			await _storageService.DeleteFolder(workspace.SessionsFolder.Id);

		_context.Remove(workspace);
		await _context.SaveChangesAsync();

		await transaction.CommitAsync();

		return true;
	}

	internal async Task<long> CloneWorkspace(Dto.CloneWorkspaceRequest request, Guid userId)
	{
		var workspace = await _context.Workspaces.FindAsync(request.WorkspaceId);
		if (workspace == null)
			return 0;

		var createWorkspaceRequest = new Dto.CreateWorkspaceRequest
		{
			Name = request.Name,
			Description = request.Description,
		};

		var workspaceId = await CreateWorkspace(createWorkspaceRequest, userId);
		if (workspaceId == 0)
			return 0;

		var newWorkspace = await _context.Workspaces.FindAsync(workspaceId);
		if (newWorkspace == null)
			return 0;

		await _context.Entry(newWorkspace).Collection(c => c.WorkspaceUsers).LoadAsync();


		if (request.CopyWorkspaceUsers)
		{
			var copiedWorkspaceUsers = await _context.WorkspaceUsers
				.AsNoTracking()
				.Where(cu => cu.WorkspaceId == request.WorkspaceId)
				.ToListAsync();

			copiedWorkspaceUsers.ForEach(cu => cu.WorkspaceId = newWorkspace.Id);

			if (!request.CopyRoles)
				copiedWorkspaceUsers.ForEach(cu => cu.Role = cu.UserId == userId ? WorkspaceUserRole.Admin : WorkspaceUserRole.Member);
			newWorkspace.WorkspaceUsers = copiedWorkspaceUsers;
		}

		if (!request.CopyWorkspaceUsers) request.CopyFoldersPermissions = false;
		if (request.CopyFolders && workspace.RootFolderId is not null)
		{
			var rootFolderCopy = await _storageService.CreateFolderCopy(workspace.RootFolderId.Value, newWorkspace.Id, request.CopyFiles,
				request.CopyFoldersPermissions
			);
			if (rootFolderCopy is not null)
			{
				await _context.Entry(newWorkspace).Reference(c => c.RootFolder).LoadAsync();
				if (newWorkspace.RootFolder is not null) _context.Remove(newWorkspace.RootFolder);
				await _context.AddAsync(rootFolderCopy);

				newWorkspace.RootFolder = rootFolderCopy;
				newWorkspace.RootFolder.Name = newWorkspace.Name;
			}
		}

		await _context.SaveChangesAsync();
		return newWorkspace.Id;
	}
}

