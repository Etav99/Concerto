using Concerto.Server.Data.DatabaseContext;
using Concerto.Server.Data.Models;
using Concerto.Server.Extensions;
using Concerto.Server.Settings;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using System.Net;
using MimeMapping;

namespace Concerto.Server.Services;

public class StorageService
{
	private readonly AppDataContext _context;
	private readonly ILogger<StorageService> _logger;
	private readonly IMemoryCache _memoryCache;

	public StorageService(ILogger<StorageService> logger, AppDataContext context, IMemoryCache memoryCache)
	{
		_logger = logger;
		_context = context;
		_memoryCache = memoryCache;
	}

	// Create
	internal async Task<long?> CreateFolder(Dto.CreateFolderRequest request, Guid ownerId)
	{
		var parent = await _context.Folders.FindAsync(request.ParentId);
		if (parent == null) return null;

		var rootParentOrNotInheriting = parent.IsWorkspaceRoot || !request.WorkspacePermission.Inherited;

		var workspacePermission = rootParentOrNotInheriting
			? request.WorkspacePermission.ToEntity(false)
			: parent.WorkspacePermission with { Inherited = true };

		var newFolder = new Folder
		{
			Name = request.Name,
			OwnerId = ownerId,
			ParentId = parent.Id,
			WorkspaceId = parent.WorkspaceId,
			WorkspacePermission = workspacePermission,
			Type = request.Type == Dto.FolderType.WorkspaceRoot ? FolderType.Other : request.Type.ToEntity()
		};

		// Inherit permissions from parent;
		await _context.Folders.AddAsync(newFolder);
		await _context.Entry(parent).Collection(p => p.UserPermissions).LoadAsync();
		await InheritPermissions(parent, newFolder, false);
		await _context.SaveChangesAsync();

		return newFolder.Id;
	}

	// Read
	internal async Task<Dto.FolderContent?> GetFolderContent(long folderId, Guid userId, bool isAdmin = false)
	{
		var folder = await _context.Folders.FindAsync(folderId);
		if (folder == null) return null;

		await _context.Entry(folder).Collection(c => c.SubFolders).LoadAsync();
		await _context.Entry(folder).Collection(c => c.Files).LoadAsync();
		await _context.Entry(folder).Reference(f => f.Owner).LoadAsync();

		// Load owners of subfolders
		await _context.Entry(folder).Collection(f => f.SubFolders).Query().Include(f => f.Owner).LoadAsync();

		// get sessions folder if workspace root
		if (folder.IsWorkspaceRoot)
		{
			var workspace = await _context.Workspaces.FindAsync(folder.WorkspaceId);
			if (workspace == null) return null;
			await _context.Entry(workspace).Reference(c => c.SessionsFolder).LoadAsync();
			if (workspace.SessionsFolder == null) return null;
			folder.SubFolders.Add(workspace.SessionsFolder);
		}

		var subFolders = new List<Dto.FolderItem>();
		foreach (var subFolder in folder.SubFolders)
		{
			if (isAdmin)
			{
				subFolders.Add(subFolder.ToFolderItem(true, true, await CanDeleteFolder(userId, subFolder.Id)));
			}
			else
			{
				var canWrite = await CanWriteInFolder(userId, subFolder.Id);
				var canEdit = await CanEditFolder(userId, subFolder.Id);
				var canDelete = await CanDeleteFolder(userId, subFolder.Id);
				subFolders.Add(subFolder.ToFolderItem(canWrite, canEdit, canDelete));
			}
		}

		var files = new List<Dto.FileItem>();

		// Load owner for each file
		await _context.Entry(folder).Collection(f => f.Files).Query().Include(f => f.Owner).LoadAsync();
		foreach (var file in folder.Files)
		{
			var canManageFile = isAdmin || await CanManageFile(userId, file.Id);
			files.Add(file.ToFileItem(canManageFile));
		}

		Dto.FolderItem selfFolderItem;
		if (isAdmin)
		{
			selfFolderItem = folder.ToFolderItem(true, true, true);
		}
		else
		{
			var canWrite = await CanWriteInFolder(userId, folder.Id);
			var canEdit = await CanEditFolder(userId, folder.Id);
			var canDelete = await CanDeleteFolder(userId, folder.Id);
			selfFolderItem = folder.ToFolderItem(canWrite, canEdit, canDelete);
		}

		return new Dto.FolderContent
		{
			Self = selfFolderItem,
			SubFolders = subFolders,
			WorkspaceId = folder.WorkspaceId,
			Files = files,
			WorkspacePermission = folder.WorkspacePermission.ToViewModel()
		};
	}

	internal async Task<Dto.FolderSettings?> GetFolderSettings(long id)
	{
		var folder = await _context.Folders.FindAsync(id);
		if (folder == null) return null;

		await _context.Entry(folder).Reference(f => f.Parent).LoadAsync();
		await _context.Entry(folder).Collection(c => c.UserPermissions).Query().Include(up => up.User).LoadAsync();

		// Join UserPermission with parent UserPermissions
		var parentUserPermissions = _context.UserFolderPermissions.Where(ufp => ufp.FolderId == folder.ParentId);

		return new Dto.FolderSettings(folder.Id,
			folder.Name,
			WorkspacePermission: folder.WorkspacePermission.ToViewModel(),
			OwnerId: folder.OwnerId,
			WorkspaceId: folder.WorkspaceId,
			ParentWorkspacePermission: folder.Parent?.WorkspacePermission.ToViewModel(),
			Type: folder.Type.ToViewModel(),
			UserPermissions: folder.UserPermissions.Select(fp => fp.ToViewModel()),
			ParentUserPermissions: parentUserPermissions.Select(fp => fp.ToViewModel())
		);
	}

	internal async Task<Dto.FileSettings?> GetFileSettings(long id)
	{
		var file = await _context.UploadedFiles.FindAsync(id);
		if (file == null) return null;

		return new Dto.FileSettings(file.Id, file.DisplayName);
	}

	internal async Task<bool> CanReadFile(Guid userId, long fileId)
	{
		var file = await _context.UploadedFiles.FindAsync(fileId);
		if (file == null) return false;
		return await CanReadInFolder(userId, file.FolderId);
	}

	private async Task<FolderPermissionType?> UserPermissionInFolder(Guid userId, Folder? folder)
	{
		if (folder == null) return null;
		// Null if user not in folder's workspace
		var workspaceUserRole = (await _context.WorkspaceUsers.FindAsync(folder.WorkspaceId, userId))?.Role;
		if (workspaceUserRole == null) return null;

		// Maximum permission if user is owner, folder's workspace admin or supervisor
		if (folder.OwnerId == userId || workspaceUserRole is WorkspaceUserRole.Supervisor or WorkspaceUserRole.Admin)
			return FolderPermissionType.Max;

		// User specific permission if exists (higher precedence than workspace permission)
		var userFolderPermissionType = (await _context.UserFolderPermissions.FindAsync(userId, folder.Id))?.Permission.Type;
		return userFolderPermissionType ?? folder.WorkspacePermission.Type;
	}

	internal async Task<bool> CanReadInFolder(Guid userId, long folderId)
	{
		var folder = await _context.Folders.FindAsync(folderId);
		var permissionType = await UserPermissionInFolder(userId, folder);
		return permissionType != null;
	}

	internal async Task<bool> CanWriteInFolder(Guid userId, long folderId)
	{
		var folder = await _context.Folders.FindAsync(folderId);
		var permissionType = await UserPermissionInFolder(userId, folder);
		return permissionType is FolderPermissionType.ReadWrite or FolderPermissionType.ReadWriteOwned;
	}

	internal async Task<bool> CanEditFolder(Guid userId, long folderId)
	{
		var folder = await _context.Folders.FindAsync(folderId);
		if (folder is null) return false;

		// If permanent then only admin or supervisor can edit
		if (folder.IsPermanent)
		{
			var workspaceUserRole = (await _context.WorkspaceUsers.FindAsync(folder.WorkspaceId, userId))?.Role;
			return workspaceUserRole is WorkspaceUserRole.Admin or WorkspaceUserRole.Supervisor;
		}

		// True if ReadWrite in folder's parent and folder
		var parentFolder = await _context.Folders.FindAsync(folderId);
		if (await UserPermissionInFolder(userId, parentFolder) == FolderPermissionType.ReadWrite)
			if (await UserPermissionInFolder(userId, folder) == FolderPermissionType.ReadWrite)
				return true;

		return false;
	}

	internal async Task<bool> CanDeleteFolder(Guid userId, long folderId)
	{
		var folder = await _context.Folders.FindAsync(folderId);
		if (folder is null) return false;
		if (folder.IsPermanent) return false;

		// True if folder owner
		if (folder.OwnerId == userId) return true;

		// True if ReadWrite in folder's parent and folder
		var parentFolder = await _context.Folders.FindAsync(folder.ParentId);
		if (await UserPermissionInFolder(userId, parentFolder) == FolderPermissionType.ReadWrite)
			if (await UserPermissionInFolder(userId, folder) == FolderPermissionType.ReadWrite)
				return true;

		return false;
	}

	internal async Task<bool> CanMoveFolder(Guid userId, long folderId)
	{
		return await CanDeleteFolder(userId, folderId);
	}

	public async Task<bool> CanManageFile(Guid userId, long fileId)
	{
		var file = await _context.UploadedFiles.FindAsync(fileId);
		if (file == null) return false;
		var folder = await _context.Folders.FindAsync(file.FolderId);
		var permission = await UserPermissionInFolder(userId, folder);
		switch (permission)
		{
			case FolderPermissionType.ReadWriteOwned when file.OwnerId == userId:
			case FolderPermissionType.ReadWrite:
				return true;
			case null:
			case FolderPermissionType.Read:
			default:
				return false;
		}
	}

	public async Task<UploadedFile?> GetFile(long fileId)
	{
		return await _context.UploadedFiles.FindAsync(fileId);
	}

	internal async Task UpdateFolder(Dto.UpdateFolderRequest request)
	{
		var folder = await _context.Folders.FindAsync(request.Id);
		if (folder == null) return;

		if (!folder.IsPermanent)
		{
			folder.Name = request.Name;
			folder.Type = request.Type == Dto.FolderType.WorkspaceRoot ? FolderType.Other : request.Type.ToEntity();
		}

		if (request.WorkspacePermission.Inherited)
			folder.WorkspacePermission = folder.WorkspacePermission with { Inherited = true };
		else
			folder.WorkspacePermission = request.WorkspacePermission.ToEntity();

		await _context.Entry(folder).Collection(f => f.UserPermissions).LoadAsync();
		var userPermissionsList = folder.UserPermissions.ToList();

		// Remove inherited permissions not present in request
		userPermissionsList.RemoveAll(up => !up.Permission.Inherited && request.UserPermissions.All(rup => rup.User.Id != up.UserId));

		foreach (var requestUserPermission in request.UserPermissions)
		{
			var match = userPermissionsList.Find(up => up.UserId == requestUserPermission.User.Id);
			// Add new permissions
			if (match == null)
				userPermissionsList.Add(requestUserPermission.ToEntity());
			// If request permission inherited then mark matching as inherited
			else if (requestUserPermission.Permission.Inherited)
				match.Permission.Inherited = true;
			// If request permission not inherited then update
			else
				match.Permission = requestUserPermission.Permission.ToEntity();
		}

		folder.UserPermissions = userPermissionsList;

		// Inherit marked permissions from parent
		await _context.Entry(folder).Reference(f => f.Parent).LoadAsync();
		if (folder.Parent != null) await InheritPermissions(folder.Parent, folder, false);

		await InheritPermissionsInChildrenRecursively(folder, request.forceInherit);
		await _context.SaveChangesAsync();
	}

	private async Task InheritPermissionsInChildrenRecursively(Folder parentFolder, bool forceInherit)
	{
		await _context.Entry(parentFolder).Collection(f => f.SubFolders).LoadAsync();
		foreach (var subFolder in parentFolder.SubFolders)
		{
			await InheritPermissions(parentFolder, subFolder, forceInherit);
			await InheritPermissionsInChildrenRecursively(subFolder, forceInherit);
		}
	}

	private async Task InheritPermissions(Folder parentFolder, Folder subFolder, bool forceInherit)
	{
		if (subFolder.WorkspacePermission.Inherited) subFolder.WorkspacePermission = parentFolder.WorkspacePermission with { Inherited = true };

		await _context.Entry(parentFolder).Collection(f => f.UserPermissions).LoadAsync();
		await _context.Entry(subFolder).Collection(f => f.UserPermissions).LoadAsync();

		var subFolderUserPermissionsList = subFolder.UserPermissions.ToList();
		subFolderUserPermissionsList.RemoveAll(up =>
			up.Permission.Inherited && parentFolder.UserPermissions.All(pup => pup.UserId != up.UserId)
		);
		subFolder.UserPermissions = subFolderUserPermissionsList;

		foreach (var parentUserPermission in parentFolder.UserPermissions)
		{
			// Find matching existing user permission in parent
			var match = subFolderUserPermissionsList.FirstOrDefault(up => up.UserId == parentUserPermission.UserId);
			// If no matching user permission inherit it
			if (match is null)
				subFolder.UserPermissions.Add(new UserFolderPermission
				{
					UserId = parentUserPermission.UserId,
					Permission = parentUserPermission.Permission with { Inherited = true }
				}
				);
			// Else if existing user permission is inherited or forcing inheritance, update it
			else if (match.Permission.Inherited || forceInherit)
				match.Permission = parentUserPermission.Permission with { Inherited = true };
			// Do nothing if matched but not inherited
		}
	}

	internal async Task DeleteFolders(IEnumerable<long> folderIds)
	{
		foreach (var folderId in folderIds) await DeleteFolder(folderId);
	}

	internal async Task DeleteFolder(long folderId)
	{
		var folder = await _context.Folders.FindAsync(folderId);
		if (folder == null) return;

		await DeleteFolderRecursively(folder);
		var deletedFiles = _context.ChangeTracker.Entries<UploadedFile>()
			.Where(e => e.State == EntityState.Deleted)
			.Select(e => e.Entity)
			.ToList();
		await _context.SaveChangesAsync();

		foreach (var file in deletedFiles)
			await DeletePhysicalFile(file);
	}

	private async Task DeleteFolderRecursively(Folder folder)
	{
		// Remove files
		await _context.Entry(folder).Collection(f => f.Files).LoadAsync();
		_context.UploadedFiles.RemoveRange(folder.Files);

		// Remove subfolders
		await _context.Entry(folder).Collection(f => f.SubFolders).LoadAsync();
		foreach (var subFolder in folder.SubFolders) await DeleteFolderRecursively(subFolder);

		_context.Folders.Remove(folder);
	}

	internal async Task DeleteFile(long fileId)
	{
		var file = await _context.UploadedFiles.FindAsync(fileId);
		if (file == null) return;

		_context.UploadedFiles.Remove(file);
		await _context.SaveChangesAsync();
		await DeletePhysicalFile(file);
	}

	internal async Task DeleteFiles(IEnumerable<long> fileIds)
	{
		var fileIdsSet = fileIds.ToHashSet();
		var files = await _context.UploadedFiles.Where(f => fileIdsSet.Contains(f.Id)).ToListAsync();
		_context.UploadedFiles.RemoveRange(files);
		await _context.SaveChangesAsync();

		foreach (var file in files)
		{
			await DeletePhysicalFile(file);
		}
	}

	internal async Task<bool> UpdateFile(Dto.UpdateFileRequest request)
	{
		var file = await _context.UploadedFiles.FindAsync(request.FileId);
		if (file == null) return false;

		file.DisplayName = WebUtility.HtmlEncode(request.Name);
		await _context.SaveChangesAsync();
		return true;
	}

	private async Task DeletePhysicalFile(UploadedFile file)
	{
		if (!await _context.UploadedFiles.AnyAsync(f => f.StorageName == file.StorageName))
			try
			{
				await FileExtensions.DeleteAsync(file.Path);
			}
			catch (DirectoryNotFoundException) { }
			catch (Exception e)
			{
				_logger.LogError("Error deleting physical file\n{e}", e);
			}
	}

	internal static bool IsFirstChunk(Dto.FileChunkMetadata fileChunk) => File.Exists(GetChunkFilePath(fileChunk));
	private static string GetChunkFilePath(Dto.FileChunkMetadata fileChunk) => Path.Combine(AppSettings.Storage.TempPath, fileChunk.Guid.ToString());

	internal async Task AbortFileUpload(Dto.FileChunkMetadata fileChunk)
	{
		try
		{
			await FileExtensions.DeleteAsync(GetChunkFilePath(fileChunk));
		}
		catch (DirectoryNotFoundException) { }
		catch (Exception e)
		{
			_logger.LogError("Error deleting temp file\n{e}", e);
		}
	}

	internal async Task<bool> SaveChunk(Dto.FileChunkMetadata fileChunk, IFormFile file)
	{
		await using (var stream = new FileStream(GetChunkFilePath(fileChunk), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.Read, 524_288, FileOptions.Asynchronous))
		{
			stream.Seek(fileChunk.Offset, SeekOrigin.Begin);
			await file.CopyToAsync(stream);
		}

		// Return true if last chunk
		return fileChunk.Offset + file.Length == fileChunk.FileSize;
	}

	internal async Task<Dto.FileUploadResult> SaveUploadedFile(Dto.FileChunkMetadata lastChunk, string fileName, Guid userId)
	{
		FileUploadResult fileUploadResult = new();

		try
		{
			var folder = await _context.Folders.FindAsync(lastChunk.FolderId);
			if (folder is null) throw new NullReferenceException("Folder is not present in database");

			var folderId = folder.Id;
			var tempFilePath = GetChunkFilePath(lastChunk);
			var tempFileInfo = new FileInfo(tempFilePath);

			if (tempFileInfo.Length != lastChunk.FileSize) throw new IOException("File size mismatch");

			var sanitizedFilename = WebUtility.HtmlEncode(fileName);
			if (string.IsNullOrEmpty(sanitizedFilename)) throw new FilenameException("Filename is empty");

			var filename = Path.GetFileNameWithoutExtension(sanitizedFilename);
			var extension = Path.GetExtension(sanitizedFilename).ToLower();

			fileUploadResult.DisplayFileName = filename;
			fileUploadResult.Extension = extension;

			var storageFileName = string.Format(@$"{sanitizedFilename}.{Guid.NewGuid()}");

			fileUploadResult.StorageFileName = storageFileName;
			fileUploadResult.StorageDir = folderId.ToString();


			var uploadedFile = new UploadedFile
			{
				OwnerId = userId,
				DisplayName = fileUploadResult.DisplayFileName,
				Extension = fileUploadResult.Extension,
				Size = lastChunk.FileSize,
				MimeType = MimeUtility.GetMimeMapping(fileUploadResult.Extension),
				StorageName = fileUploadResult.StorageFileName,
				FolderId = folderId
			};

			await FileExtensions.MoveAsync(tempFilePath, uploadedFile.Path, true);

			await _context.AddAsync(uploadedFile);
			await _context.SaveChangesAsync();
			fileUploadResult.Uploaded = true;
		}
		catch (IOException ex)
		{
			_logger.LogError("{fileName} error on upload: {message}", fileName, ex.Message);
			fileUploadResult.ErrorCode = 3;
			fileUploadResult.ErrorMessage = "File upload failed";
		}
		catch (FilenameException ex)
		{
			_logger.LogError("{fileName} error on upload: {message}", fileName, ex.Message);
			fileUploadResult.ErrorCode = 4;
			fileUploadResult.ErrorMessage = "File upload failed";
		}
		catch
		{
			await AbortFileUpload(lastChunk);
			throw;
		}

		return fileUploadResult.ToViewModel();
	}

	internal async Task CopyFiles(IEnumerable<long> fileIds, long destinationFolderId, Guid userId)
	{
		var destinationFolder = await _context.Folders.FindAsync(destinationFolderId);
		if (destinationFolder is null) return;

		var fileIdSet = fileIds.ToHashSet();

		await _context.Entry(destinationFolder).Collection(f => f.Files).LoadAsync();

		var files = await _context.UploadedFiles
			.AsNoTracking()
			.Where(f => fileIdSet.Contains(f.Id))
			.ToListAsync();

		foreach (var file in files)
		{
			file.Id = 0;
			file.OwnerId = userId;
			destinationFolder.Files.Add(file);
		}

		await _context.SaveChangesAsync();
	}

	internal async Task CopyFolders(IEnumerable<long> folderIds, long destinationFolderId, Guid userId)
	{
		var destinationFolder = await _context.Folders.FindAsync(destinationFolderId);
		if (destinationFolder is null) return;

		var folderIdsList = folderIds.ToList();

		await _context.Entry(destinationFolder).Collection(f => f.SubFolders).LoadAsync();

		foreach (var folderId in folderIdsList)
		{
			var folderCopy = await CreateFolderCopy(folderId, destinationFolder.WorkspaceId, true, false, userId);
			if (folderCopy is not null) destinationFolder.SubFolders.Add(folderCopy);
		}

		await _context.SaveChangesAsync();
	}

	internal async Task<Folder?> CreateFolderCopy(long folderId, long workspaceId, bool withFiles, bool withFolderPermissions, Guid? newOwner = null)
	{
		var folderCopy = await _context.Folders.AsNoTracking().SingleOrDefaultAsync(f => f.Id == folderId);
		if (folderCopy == null)
			return null;

		await CopyFolderRecursively(folderCopy, workspaceId, withFiles, withFolderPermissions, newOwner);
		return folderCopy;
	}

	private async Task CopyFolderRecursively(Folder copiedFolder, long workspaceId, bool withFiles, bool withFolderPermissions, Guid? newOwner = null)
	{
		var subFolders = await _context.Folders
			.AsNoTracking()
			.Where(f => f.ParentId == copiedFolder.Id)
			.ToListAsync();

		foreach (var subFolder in subFolders) await CopyFolderRecursively(subFolder, workspaceId, withFiles, withFolderPermissions);

		if (withFolderPermissions)
		{
			var permissions = await _context.UserFolderPermissions
				.AsNoTracking()
				.Where(fp => fp.FolderId == copiedFolder.Id)
				.ToListAsync();

			permissions.ForEach(ufp => ufp.FolderId = 0);
			copiedFolder.UserPermissions = permissions;
		}

		if (withFiles)
		{
			var files = await _context.UploadedFiles
				.AsNoTracking()
				.Where(f => f.FolderId == copiedFolder.Id)
				.ToListAsync();

			files.ForEach(f =>
				{
					f.Id = 0;
					if (newOwner.HasValue) f.OwnerId = newOwner.Value;
				}
			);

			copiedFolder.Files = files;
		}

		copiedFolder.SubFolders = subFolders;
		copiedFolder.Id = 0;
		copiedFolder.ParentId = null;
		copiedFolder.WorkspaceId = workspaceId;
		if (newOwner.HasValue) copiedFolder.OwnerId = newOwner.Value;
		if (copiedFolder.Type is FolderType.Sessions) copiedFolder.Type = FolderType.Recordings;
	}

	internal async Task MoveFiles(IEnumerable<long> fileIds, long destinationFolderId)
	{
		var destinationFolder = await _context.Folders.FindAsync(destinationFolderId);
		if (destinationFolder is null) return;

		var fileIdSet = fileIds.ToHashSet();
		var files = await _context.UploadedFiles.Where(f => fileIdSet.Contains(f.Id)).ToListAsync();
		files.ForEach(f => f.FolderId = destinationFolderId);
		await _context.SaveChangesAsync();
	}

	internal async Task MoveFolders(IEnumerable<long> folderIds, long destinationFolderId)
	{
		var destinationFolder = await _context.Folders.FindAsync(destinationFolderId);
		if (destinationFolder is null) return;

		foreach (var folderId in folderIds) await MoveFolder(folderId, destinationFolderId, destinationFolder.WorkspaceId);
		await _context.SaveChangesAsync();
	}

	private async Task MoveFolder(long folderId, long destinationFolderId, long? workspaceId = null)
	{
		var movedFolder = await _context.Folders.FindAsync(folderId);
		if (movedFolder == null)
			return;

		movedFolder.ParentId = destinationFolderId;
		if (workspaceId.HasValue && movedFolder.WorkspaceId != workspaceId)
		{
			HashSet<long> updatedFoldersIds = new();
			await UpdateFolderWorkspaceRecursively(movedFolder, workspaceId.Value, updatedFoldersIds);

			// Remove user specific permissions if the folder is moved to another workspace
			var userPermissions = await _context.UserFolderPermissions
				.Where(ufp => updatedFoldersIds.Contains(ufp.FolderId))
				.ToListAsync();
			_context.UserFolderPermissions.RemoveRange(userPermissions);
		}
	}

	private async Task UpdateFolderWorkspaceRecursively(Folder folder, long workspaceId, HashSet<long> updatedFoldersIds)
	{
		folder.WorkspaceId = workspaceId;
		updatedFoldersIds.Add(folder.Id);

		var subFolders = await _context.Folders
			.Where(f => f.ParentId == folder.Id)
			.ToListAsync();

		foreach (var subFolder in subFolders)
			await UpdateFolderWorkspaceRecursively(subFolder, workspaceId, updatedFoldersIds);
	}

	internal async Task<bool> CanFolderBeMoved(long folderId, long destinationFolderId)
	{
		var folder = await _context.Folders.FindAsync(folderId);
		if (folder is null) return false;
		var destinationFolder = await _context.Folders.FindAsync(destinationFolderId);
		if (destinationFolder is null) return false;

		if (folder.WorkspaceId != destinationFolder.WorkspaceId) return true;

		long? parentId = destinationFolder.ParentId;
		while (parentId != null)
		{
			if (parentId == folderId) return false;
			var parentFolder = await _context.Folders.FindAsync(parentId);
			if (parentFolder == null) return false;
			parentId = parentFolder.ParentId;
		}
		return true;
	}

	internal async Task<bool> CanFileBeMoved(long fileId, long destinationFolderId)
	{
		var file = await _context.UploadedFiles.FindAsync(fileId);
		if (file == null) return false;
		return file.FolderId != destinationFolderId;
	}

	internal async Task SaveRecording(string meetingId, string filePath)
	{
		var fileInfo = new FileInfo(filePath);
		if (!fileInfo.Exists) throw new FileNotFoundException("File not found", filePath);

		var meetingGuid = Guid.Parse(meetingId);
		var session = await _context.Sessions.Where(s => s.MeetingGuid == meetingGuid).FirstAsync();

		var storageName = $"{fileInfo.Name}.{Guid.NewGuid()}";
		var timeStamp = fileInfo.CreationTime.ToString("dd-MM-yyyy HH-mm-ss");
		var newFilename = $"{session.Name} {timeStamp}";
		foreach (char c in Path.GetInvalidFileNameChars())
		{
		   newFilename = newFilename.Replace(c, '_');
		}

		var recordingFile = new UploadedFile
		{
			OwnerId = null,
			DisplayName = newFilename,
			Extension = fileInfo.Extension,
			MimeType = MimeUtility.GetMimeMapping(fileInfo.Name),
			Size = fileInfo.Length,
			StorageName = storageName,
			FolderId = session.FolderId,
		};

		await FileExtensions.CopyAsync(fileInfo.FullName, recordingFile.Path, true);
		try
		{
			await _context.AddAsync(recordingFile);
			await _context.SaveChangesAsync();
		}
		catch
		{
			await FileExtensions.DeleteAsync(recordingFile.Path);
			throw;
		}

		if(fileInfo.DirectoryName != null)
			Directory.Delete(fileInfo.DirectoryName, true);
	}
}
