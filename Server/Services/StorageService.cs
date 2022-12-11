using Concerto.Server.Data.DatabaseContext;
using Concerto.Server.Data.Models;
using Concerto.Server.Extensions;
using Concerto.Server.Settings;
using Microsoft.EntityFrameworkCore;
using System.Net;

namespace Concerto.Server.Services;

public class StorageService
{
    private readonly ILogger<StorageService> _logger;

    private readonly AppDataContext _context;
    public StorageService(ILogger<StorageService> logger, AppDataContext context)
    {
        _logger = logger;
        _context = context;
    }

    // Create
    internal async Task<bool> CreateFolder(Dto.CreateFolderRequest request, long ownerId)
    {
        var parent = await _context.Folders.FindAsync(request.ParentId);
        if (parent == null) return false;

        var RootParentOrNotInheriting = (parent.IsCourseRoot || !request.CoursePermission.Inherited);
		
		var coursePermission = RootParentOrNotInheriting ? request.CoursePermission.ToEntity(inherited: false)
														 : parent.CoursePermission with { Inherited = true };

        var newFolder = new Folder()
        {
            Name = request.Name,
            OwnerId = ownerId,
            ParentId = parent.Id,
            CourseId = parent.CourseId,
            CoursePermission = coursePermission,
            Type = request.Type == Dto.FolderType.CourseRoot ? FolderType.Other : request.Type.ToEntity()
		};

		// Inherit permissions from parent;
		await _context.Folders.AddAsync(newFolder);
        await _context.Entry(parent).Collection(p => p.UserPermissions).LoadAsync();
		await InheritPermissions(parent, newFolder, false);
		await _context.SaveChangesAsync();
        return true;
    }

    // Read
    internal async Task<Dto.FolderContent?> GetFolderContent(long folderId, long userId, bool isAdmin = false)
    {
        var folder = await _context.Folders.FindAsync(folderId);
        if (folder == null) return null;

		await _context.Entry(folder).Collection(c => c.SubFolders).LoadAsync();
		await _context.Entry(folder).Collection(c => c.Files).LoadAsync();

        var subFolders = new List<Dto.FolderItem>();
        foreach (var subFolder in folder.SubFolders)
        {
            if (isAdmin)
            {
                subFolders.Add(subFolder.ToFolderItem(canWrite: true, canEdit: true, canDelete: true));
            }
            else
            {
                var canWrite = CanWriteInFolder(userId, subFolder.Id);
                var canEdit = CanEditFolder(userId, subFolder.Id);
                var canDelete = CanDeleteFolder(userId, subFolder.Id);
                subFolders.Add(subFolder.ToFolderItem(canWrite.Result, canEdit.Result, canDelete.Result));
            }
		}

		var files = new List<Dto.FileItem>();
		foreach (var file in folder.Files)
		{
			var canManageFile = isAdmin ? true : await CanManageFile(userId, file.Id);
            files.Add(file.ToFileItem(canManageFile));
		}

        Dto.FolderItem selfFolderItem;                                       
        if (isAdmin)
        {
            selfFolderItem = folder.ToFolderItem(canWrite: true, canEdit: true, canDelete: true);
        }
        else
        {
            var canWrite = CanWriteInFolder(userId, folder.Id);
            var canEdit = CanEditFolder(userId, folder.Id);
            var canDelete = CanDeleteFolder(userId, folder.Id);
            selfFolderItem = folder.ToFolderItem(canWrite.Result, canEdit.Result, canDelete.Result);
        }

		return new Dto.FolderContent()
        {
            Self = selfFolderItem,
            SubFolders = subFolders,
			CourseId = folder.CourseId,
			Files = files,
            CoursePermission = folder.CoursePermission.ToViewModel(),
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

        return new Dto.FolderSettings
        (
            Id: folder.Id,
            Name: folder.Name,
            CoursePermission: folder.CoursePermission.ToViewModel(),
            OwnerId: folder.OwnerId,
            CourseId: folder.CourseId,
            ParentCoursePermission: folder.Parent?.CoursePermission.ToViewModel(),
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


    public async Task<IEnumerable<Dto.FileUploadResult>> AddFilesToFolder(IEnumerable<IFormFile> files, long folderId)
    {
        var fileUploadResults = await SaveUploadedFiles(files, folderId);

        foreach (var fileUploadResult in fileUploadResults)
        {
            if (fileUploadResult.Uploaded)
            {
                var uploadedFile = new UploadedFile()
                {
                    DisplayName = fileUploadResult.DisplayFileName,
                    Extension = fileUploadResult.Extension,
                    StorageName = fileUploadResult.StorageFileName,
                    FolderId = folderId
                };
                _context.Add(uploadedFile);
            }
        }

        _context.SaveChanges();
        return fileUploadResults.ToViewModel();
    }

    private async Task<IEnumerable<FileUploadResult>> SaveUploadedFiles(IEnumerable<IFormFile> files, long folderId)
    {
        var maxAllowedFiles = 5;
        long maxFileSize = 1024 * 1024 * 15;
        var filesProcessed = 0;
        List<FileUploadResult> fileUploadResults = new();

        foreach (IFormFile file in files)
        {
            var fileUploadResult = new FileUploadResult();
            if (filesProcessed < maxAllowedFiles)
            {
                if (file.Length > maxFileSize)
                {
                    fileUploadResult.ErrorCode = 2;
                }
                else
                {
                    try
                    {
						var sanitizedFilename = WebUtility.HtmlEncode(file.FileName);
						if (string.IsNullOrEmpty(sanitizedFilename)) throw new FilenameException("Filename is empty");
					
						var filename = Path.GetFileNameWithoutExtension(sanitizedFilename);
                        var extension = Path.GetExtension(sanitizedFilename).ToLower();

						fileUploadResult.DisplayFileName = filename;
						fileUploadResult.Extension = extension;

						string storageFileName = string.Format(@$"{sanitizedFilename}.{Guid.NewGuid()}");
                        fileUploadResult.StorageFileName = storageFileName;
                        string path = Path.Combine($"{AppSettings.Storage.StoragePath}", $"{folderId}", storageFileName);
                        Directory.CreateDirectory(Path.Combine($"{AppSettings.Storage.StoragePath}", $"{folderId}"));
                        await using FileStream fs = new(path, FileMode.Create);
                        await file.CopyToAsync(fs);
                        fileUploadResult.Uploaded = true;
                    }
                    catch (IOException ex)
                    {
                        _logger.LogError($"{file.FileName} error on upload: {ex.Message}");
                        fileUploadResult.ErrorCode = 3;
                    }
					catch (FilenameException ex)
                    {
						_logger.LogError($"{file.FileName} error on upload: {ex.Message}");
						fileUploadResult.ErrorCode = 4;
					}

				}
                filesProcessed++;
            }
            else
            {
                _logger.LogInformation($"{file.FileName} skipped, too many files uploaded at once");
                fileUploadResult.ErrorCode = 5;
            }
            fileUploadResults.Add(fileUploadResult);
        }
        return fileUploadResults;
    }

    internal async Task<bool> CanReadFile(long userId, long fileId)
    {
        var file = await _context.UploadedFiles.FindAsync(fileId);
        if (file == null) return false;
        return await CanReadInFolder(userId, file.FolderId);
    }
    
    private async Task<FolderPermissionType?> UserPermissionInFolder(long userId, Folder? folder)
    {
        if (folder == null) return null;
        // Null if user not in folder's course
        var courseUserRole = (await _context.CourseUsers.FindAsync(folder.CourseId, userId))?.Role;
        if (courseUserRole == null) return null;

        // Maxiumum permission if user is owner, folder's course admin or supervisor
        if (folder.OwnerId == userId || courseUserRole == CourseUserRole.Supervisor || courseUserRole == CourseUserRole.Admin) return FolderPermissionType.Max;

        // User specific permission if exists (higher precedence than course permission)
        var userFolderPermissionType = (await _context.UserFolderPermissions.FindAsync(userId, folder.Id))?.Permission.Type;
        if (userFolderPermissionType != null) return userFolderPermissionType;

        return folder.CoursePermission.Type;
    }

    internal async Task<bool> CanReadInFolder(long userId, long folderId)
    {
        var folder = await _context.Folders.FindAsync(folderId);
        var permissionType = await UserPermissionInFolder(userId, folder);
        if (permissionType != null) return true;
        return false;
    }

    internal async Task<bool> CanWriteInFolder(long userId, long folderId)
    {
        var folder = await _context.Folders.FindAsync(folderId);
        var permissionType = await UserPermissionInFolder(userId, folder);
        if (permissionType == FolderPermissionType.ReadWrite || permissionType == FolderPermissionType.ReadWriteOwned) return true;
        return false;
    }
    internal async Task<bool> CanEditFolder(long userId, long folderId)
    {
        var folder = await _context.Folders.FindAsync(folderId);
        if (folder is null) return false;

        // If root then only admin or supervisor can edit
        if (folder.IsCourseRoot)
        {
            var courseUserRole = (await _context.CourseUsers.FindAsync(folder.CourseId, userId))?.Role;
            if (courseUserRole == CourseUserRole.Admin || courseUserRole == CourseUserRole.Supervisor) return true;
            return false;
        }

        // True if ReadWrite in folder's parent and folder
        var parentFolder = await _context.Folders.FindAsync(folderId);
        if (await UserPermissionInFolder(userId, parentFolder) == FolderPermissionType.ReadWrite)
        {
            if (await UserPermissionInFolder(userId, folder) == FolderPermissionType.ReadWrite) return true;
        }

        return false;
    }

    internal async Task<bool> CanDeleteFolder(long userId, long folderId)
    {
        var folder = await _context.Folders.FindAsync(folderId);
        if (folder is null) return false;
        if (folder.IsCourseRoot) return false;

        // True if folder owner
        if (folder.OwnerId == userId) return true;

        // True if ReadWrite in folder's parent and folder
        var parentFolder = await _context.Folders.FindAsync(folder.ParentId);
        if (await UserPermissionInFolder(userId, parentFolder) == FolderPermissionType.ReadWrite)
        {
            if (await UserPermissionInFolder(userId, folder) == FolderPermissionType.ReadWrite) return true;
        }
            
        return false;
    }

	internal async Task<bool> CanMoveFolder(long userId, long folderId)
	{
		var folder = await _context.Folders.FindAsync(folderId);
		if (folder is null) return false;
		if (folder.IsCourseRoot) return false;

		// True if ReadWrite in folder's parent or ReadWriteOwned in folder's parent and owns folder
		var parentFolder = await _context.Folders.FindAsync(folder.ParentId);
		var parentFolderPermission = await UserPermissionInFolder(userId, parentFolder);
		if (parentFolderPermission == FolderPermissionType.ReadWrite) return true;
        if (parentFolderPermission == FolderPermissionType.ReadWriteOwned || folder.OwnerId == userId) return true;
		return false;
	}

	public async Task<bool> CanManageFile(long userId, long fileId)
	{
		var file = _context.UploadedFiles.Find(fileId);
        if (file == null) return false;
		var folder = await _context.Folders.FindAsync(file.FolderId);
        var permission = await UserPermissionInFolder(userId, folder);
		if(permission == FolderPermissionType.ReadWriteOwned)
        {
			if (file.OwnerId == userId) return true;
		}
		if (permission == FolderPermissionType.ReadWrite) return true;
        return false;
	}

	public async Task<UploadedFile?> GetFile(long fileId)
	{
		return await _context.UploadedFiles.FindAsync(fileId);
	}

	internal async Task UpdateFolder(Dto.UpdateFolderRequest request)
    {
        var folder = await _context.Folders.FindAsync(request.Id);
        if (folder == null) return;

        folder.Name = request.Name;
        folder.Type = request.Type == Dto.FolderType.CourseRoot ? FolderType.Other : request.Type.ToEntity();
        
        if (request.CoursePermission.Inherited)
        {
            folder.CoursePermission = folder.CoursePermission with { Inherited = true };
        }
        else
        {
            folder.CoursePermission = request.CoursePermission.ToEntity();
        }

        await _context.Entry(folder).Collection(f => f.UserPermissions).LoadAsync();
        var userPermissionsList = folder.UserPermissions.ToList();
        
        // Remove inherited permissions not present in request
        userPermissionsList.RemoveAll(up => !up.Permission.Inherited && !request.UserPermissions.Any(rup => rup.User.Id == up.UserId));

        foreach (var requestUserPermission in request.UserPermissions)
        {
            var match = userPermissionsList.Find(up => up.UserId == requestUserPermission.User.Id);
            // Add new permissions
            if (match == null)
            {
                userPermissionsList.Add(requestUserPermission.ToEntity());
            }
            // If request permission inherited then mark matching as inherited
            else if (requestUserPermission.Permission.Inherited)
            {
                match.Permission.Inherited = true;
            }
            // If request permission not inherited then update
            else
            {
                match.Permission = requestUserPermission.Permission.ToEntity();
            }
        }

        folder.UserPermissions = userPermissionsList;

        // Inherit marked permissions from parent
        _context.Entry(folder).Reference(f => f.Parent).Load();
        if(folder.Parent != null) await InheritPermissions(folder.Parent, folder, false);

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
		if (subFolder.CoursePermission.Inherited)
		{
			subFolder.CoursePermission = parentFolder.CoursePermission with { Inherited = true };
		}
        
        _context.Entry(parentFolder).Collection(f => f.UserPermissions).Load();
        await _context.Entry(subFolder).Collection(f => f.UserPermissions).LoadAsync();

        var subFolderUserPermissionsList = subFolder.UserPermissions.ToList();
        subFolderUserPermissionsList.RemoveAll(up => up.Permission.Inherited && !parentFolder.UserPermissions.Any(pup => pup.UserId == up.UserId));
        subFolder.UserPermissions = subFolderUserPermissionsList;

        foreach (var parentUserPermission in parentFolder.UserPermissions)
		{
			// Find matching existing user permission in parent
			var match = subFolderUserPermissionsList.FirstOrDefault(up => up.UserId == parentUserPermission.UserId);
			// If no matching user permission inherit it
			if (match is null)
			{
				subFolder.UserPermissions.Add(new UserFolderPermission
				{
					UserId = parentUserPermission.UserId,
					Permission = parentUserPermission.Permission with { Inherited = true }
				});
			}
			// Else if existing user permission is inherited or forcing inheritance, update it
			else if (match.Permission.Inherited || forceInherit)
			{
				match.Permission = parentUserPermission.Permission with { Inherited = true };
			}
            // Do nothing if matched but not inherited
		}
    }

	internal async Task DeleteFolders(IEnumerable<long> folderIds)
	{
        foreach (var folderId in folderIds)
        {
            await DeleteFolder(folderId);
        }
	}

	internal async Task DeleteFolder(long folderId)
	{
		var folder = await _context.Folders.FindAsync(folderId);
		if (folder == null) return;

        await DeleteFolderRecursively(folder);
		var deletedFiles = _context.ChangeTracker.Entries<UploadedFile>().Where(e => e.State == EntityState.Deleted).Select(e => e.Entity).ToList();
		await _context.SaveChangesAsync();

        var deleteTasks = deletedFiles.Select(f => DeletePhysicalFile(f));
		await Task.WhenAll(deleteTasks);
	}
	private async Task DeleteFolderRecursively(Folder folder)
	{
        // Remove files
        await _context.Entry(folder).Collection(f => f.Files).LoadAsync();
		_context.UploadedFiles.RemoveRange(folder.Files);

		// Remove subfolders
		await _context.Entry(folder).Collection(f => f.SubFolders).LoadAsync();
		foreach (var subFolder in folder.SubFolders)
		{
			await DeleteFolderRecursively(subFolder);
		}

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
		var files = await _context.UploadedFiles.Where(f => fileIds.Contains(f.Id)).ToListAsync();
		_context.UploadedFiles.RemoveRange(files);
		await _context.SaveChangesAsync();
		
		var deleteTasks = files.Select(f => DeletePhysicalFile(f));
		await Task.WhenAll(deleteTasks);
	}

	internal async Task<bool> UpdateFile(Dto.UpdateFileRequest request)
    {
        var file = await _context.UploadedFiles.FindAsync(request.FileId);
        if (file == null) return false;

        file.DisplayName =  WebUtility.HtmlEncode(request.Name);
        await _context.SaveChangesAsync();
        return true;
    }
	
	private async Task DeletePhysicalFile(UploadedFile file)
	{
		if(!await _context.UploadedFiles.AnyAsync(f => f.StorageName == file.StorageName))
        {
            System.IO.File.Delete(file.Path);
		}
	}

	internal async Task CopyFiles(IEnumerable<long> fileIds, long destinationFolderId, long userId)
	{
		var destinationFolder = await _context.Folders.FindAsync(destinationFolderId);
		if (destinationFolder is null) return;
		
		var fileIdSet = fileIds.ToHashSet();
		
		await _context.Entry(destinationFolder).Collection(f => f.Files).LoadAsync();

        var files = await _context.UploadedFiles
            .AsNoTracking()
            .Where(f => fileIdSet.Contains(f.Id))
			.ToListAsync();

		foreach(var file in files)
        {
			file.Id = 0;
			file.OwnerId = userId;
            destinationFolder.Files.Add(file);
		}
        await _context.SaveChangesAsync();
	}

	internal async Task CopyFolders(IEnumerable<long> folderIds, long destinationFolderId, long userId)
    {
		var destinationFolder = await _context.Folders.FindAsync(destinationFolderId);
        if (destinationFolder is null) return;
		
		var folderIdsList = folderIds.ToList();
		
        await _context.Entry(destinationFolder).Collection(f => f.SubFolders).LoadAsync();

		foreach(var folderId in folderIdsList)
        {
            var folderCopy = await CreateFolderCopy(folderId, destinationFolder.CourseId, true, false, userId);
			if (folderCopy is not null) destinationFolder.SubFolders.Add(folderCopy);
		}
        await _context.SaveChangesAsync();
	}

	internal async Task<Folder?> CreateFolderCopy(long folderId, long courseId, bool withFiles, bool withFolderPermissions, long? newOwner = null)
    {
        var folderCopy = await _context.Folders.FindAsync(folderId);
        if (folderCopy == null)
            return null;
        _context.Entry(folderCopy).State = EntityState.Detached;

        await CopyFolderRecursively(folderCopy, courseId, withFiles, withFolderPermissions, newOwner);
        return folderCopy;
    }

    internal async Task CopyFolderRecursively(Folder copiedFolder, long courseId, bool withFiles, bool withFolderPermissions, long? newOwner = null)
    {
        var subFolders = await _context.Folders
            .AsNoTracking()
            .Where(f => f.ParentId == copiedFolder.Id)
            .ToListAsync();

        foreach (var subFolder in subFolders)
        {
            await CopyFolderRecursively(subFolder, courseId, withFiles, withFolderPermissions);
        }

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
			});
			
			copiedFolder.Files = files;
        }

        copiedFolder.SubFolders = subFolders;
        copiedFolder.Id = 0;
        copiedFolder.ParentId = 0;
        copiedFolder.CourseId = courseId;
		if (newOwner.HasValue) copiedFolder.OwnerId = newOwner.Value;
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
		
		foreach (var folderId in folderIds)
		{
			await MoveFolder(folderId, destinationFolderId, destinationFolder.CourseId);
		}
        await _context.SaveChangesAsync();
	}

	internal async Task MoveFolder(long folderId, long destinationFolderId, long? courseId = null)
	{
		var movedFolder = await _context.Folders.FindAsync(folderId);
		if (movedFolder == null)
			return;

        movedFolder.ParentId = destinationFolderId;
		if (courseId.HasValue && movedFolder.CourseId != courseId)
        {
			HashSet<long> updatedFoldersIds = new();
			await UpdateFolderCourseRecursively(movedFolder, courseId.Value, updatedFoldersIds);

			// Remove user specific permissions if the folder is moved to another course
			var userPermissions = await _context.UserFolderPermissions
				.Where(ufp => updatedFoldersIds.Contains(ufp.FolderId))
				.ToListAsync();
			_context.UserFolderPermissions.RemoveRange(userPermissions);
		}
	}
	
	internal async Task UpdateFolderCourseRecursively(Folder folder, long courseId, HashSet<long> updatedFoldersIds)
	{
		folder.CourseId = courseId;
		updatedFoldersIds.Add(folder.Id);
		
		var subFolders = await _context.Folders
			.Where(f => f.ParentId == folder.Id)
			.ToListAsync();

		foreach (var subFolder in subFolders)
		{
			await UpdateFolderCourseRecursively(subFolder, courseId, updatedFoldersIds);
		}
	}

}
