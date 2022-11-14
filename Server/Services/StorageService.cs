using Concerto.Server.Data.DatabaseContext;
using Concerto.Server.Data.Models;
using Concerto.Server.Extensions;
using Concerto.Server.Settings;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics.SymbolStore;
using System.Linq.Expressions;
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
    internal async Task CreateFolder(Dto.CreateFolderRequest createFolderRequest, long ownerId)
    {
        await _context.Folders.AddAsync(new Folder()
        {
            Name = createFolderRequest.Name,
            OwnerId = ownerId,
            CoursePermission = createFolderRequest.CoursePermission.ToEntity(),
            UserPermissions = createFolderRequest.UserPermissions.Select(up => up.ToEntity()).ToList(),
        });
        await _context.SaveChangesAsync();
    }

    // Read
    internal async Task<Dto.FolderContent?> GetFolderContent(long folderId, long userId)
    {
        var folder = await _context.Folders.FindAsync(folderId);
        if (folder == null) return null;

		await _context.Entry(folder).Collection(c => c.SubFolders).LoadAsync();
		await _context.Entry(folder).Collection(c => c.Files).LoadAsync();


        var subFolders = new List<Dto.FolderItem>();
        foreach (var subFolder in folder.SubFolders)
        {
            subFolders.Add(new Dto.FolderItem
            {
				Id = subFolder.Id,
				Name = subFolder.Name,
				CanWrite = await CanWriteInFolder(userId, subFolder.Id),
				CanEdit = await CanEditFolder(userId, subFolder.Id),
				CanDelete = await CanDeleteFolder(userId, subFolder.Id),
			});
		}

		var files = new List<Dto.FileItem>();
		foreach (var file in folder.Files)
		{
			var canManageFile = await CanManageFile(userId, file.Id);
			files.Add(new Dto.FileItem()
			{
				Id = file.Id,
				Name = file.DisplayName,
				CanEdit = canManageFile,
				CanDelete = canManageFile,
			});
		}

        var folderContent = new Dto.FolderContent()
        {
            Id = folder.Id,
            Self = new Dto.FolderItem
            {
                Id = folder.Id,
                CanWrite = await CanWriteInFolder(userId, folder.Id),
                CanEdit = await CanEditFolder(userId, folder.Id),
                CanDelete = await CanDeleteFolder(userId, folder.Id),
            },
            SubFolders = subFolders,
            Files = files
        };

		return folderContent;
	}

	internal async Task<Dto.FolderSettings?> GetFolderSettings(long id)
    {
        var folder = await _context.Folders.FindAsync(id);
        if (folder == null) return null;
        
        await _context.Entry(folder).Collection(c => c.UserPermissions).Query().Include(sis => sis.User).LoadAsync();
        return folder.ToFolderSettings();
    }


    public async Task<IEnumerable<Dto.FileUploadResult>> AddFilesToFolder(IEnumerable<IFormFile> files, long folderId)
    {
        var fileUploadResults = await SaveUploadedFiles(files, folderId);

        foreach (var fileUploadResult in fileUploadResults)
        {
            if (fileUploadResult.Uploaded && !string.IsNullOrEmpty(fileUploadResult.DisplayFileName) && !string.IsNullOrEmpty(fileUploadResult.StorageFileName))
            {
                var uploadedFile = new UploadedFile()
                {
                    DisplayName = fileUploadResult.DisplayFileName,
                    StorageName = fileUploadResult.StorageFileName,
                    FolderId = folderId
                };
                _context.Add(uploadedFile);
            }
        }

        _context.SaveChanges();
        return fileUploadResults.ToDto();
    }

    public async Task<IEnumerable<FileUploadResult>> SaveUploadedFiles(IEnumerable<IFormFile> files, long folderId)
    {
        var maxAllowedFiles = 5;
        long maxFileSize = 1024 * 1024 * 15;
        var filesProcessed = 0;
        List<FileUploadResult> fileUploadResults = new();

        foreach (IFormFile file in files)
        {
            var fileUploadResult = new FileUploadResult();
            var sanitizedDisplayFileName = WebUtility.HtmlEncode(file.FileName);
            fileUploadResult.DisplayFileName = sanitizedDisplayFileName;

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
                        string storageFileName = string.Format(@$"{sanitizedDisplayFileName}.{Guid.NewGuid()}");
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
                }
                filesProcessed++;
            }
            else
            {
                _logger.LogInformation($"{file.FileName} skipped, too many files uploaded at once");
                fileUploadResult.ErrorCode = 4;
            }
            fileUploadResults.Add(fileUploadResult);
        }
        return fileUploadResults;
    }

    internal async Task<bool> HasFileReadAccess(long userId, long fileId)
    {
        var file = await _context.UploadedFiles.FindAsync(fileId);
        if (file == null) return false;
        return await CanReadInFolder(userId, file.FolderId);
    }
    
    private async Task<FolderPermissionType?> UserPermissionInFolder(long userId, Folder? folder)
    {
        if (folder == null) return null;
        // Null if user not in folder's course
        var courseUserRole = (await _context.CourseUsers.FindAsync(userId, folder.CourseId))?.Role;
        if (courseUserRole == null) return null;

        // Maxiumum permission if user is owner, folder's course admin or supervisor
        if (folder.OwnerId == userId || courseUserRole == CourseUserRole.Supervisor || courseUserRole == CourseUserRole.Admin) return FolderPermissionType.Max;

        // User specific permission if exists (higher precedence than course permission)
        var userFolderPermissionType = (await _context.UserFolderPermissions.FindAsync(UserFolderPermission.ToKey(userId, folder.Id)))?.Permission.Type;
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
        if (folder.IsCourseRoot) return false;

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
        var parentFolder = await _context.Folders.FindAsync(folderId);
        if (await UserPermissionInFolder(userId, parentFolder) == FolderPermissionType.ReadWrite)
        {
            if (await UserPermissionInFolder(userId, folder) == FolderPermissionType.ReadWrite) return true;
        }
            
        return false;
    }
	
	private async Task<bool> CanManageFile(long userId, long fileId)
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

	internal async Task UpdateFolder(Dto.UpdateFolderRequest updateFolderRequest)
    {
        var folder = await _context.Folders.FindAsync(updateFolderRequest.Id);
        if (folder == null) return;

        folder.Name = updateFolderRequest.Name;

        var newCoursePermissions = updateFolderRequest.CoursePermission.ToEntity();
        var newUserPermissions = updateFolderRequest.UserPermissions.Select(up => up.ToEntity()).ToList();

        folder.CoursePermission = newCoursePermissions;
        await _context.Entry(folder).Collection(f => f.UserPermissions).LoadAsync();
        folder.UserPermissions = newUserPermissions;

        newCoursePermissions = new FolderPermission
        {
            Type = newCoursePermissions.Type,
            Inherited = true
        };
        
        await UpdateSubFoldersPermissionsRecursively(folder, newCoursePermissions, newUserPermissions);

        await _context.SaveChangesAsync();
    }
    
    private async Task UpdateSubFoldersPermissionsRecursively(Folder folder, FolderPermission? newCoursePermission, ICollection<UserFolderPermission> newUserPermissions)
    {
        await _context.Entry(folder).Collection(f => f.SubFolders).LoadAsync();
        var subFolders = folder.SubFolders;
        foreach (var subFolder in subFolders)
        {
            var newCoursePermissionCopy = newCoursePermission;
            var newUserPermissionsCopy = new List<UserFolderPermission>(newUserPermissions);

            if (newCoursePermissionCopy is not null)
            {
                if (subFolder.CoursePermission.Inherited)
                {
                    subFolder.CoursePermission.Type = newCoursePermissionCopy.Type;
                }
                else
                {
                    newCoursePermissionCopy = null;
                }
            }

            // Replace existing inherited permission for users
            await _context.Entry(folder).Collection(f => f.UserPermissions).LoadAsync();

            foreach (var userPermission in subFolder.UserPermissions)
            {
                if (userPermission.Permission.Inherited)
                {
                    var newUserPermission = newUserPermissionsCopy.FirstOrDefault(up => up.UserId == userPermission.UserId);
                    if (newUserPermission is not null)
                    {
                        userPermission.Permission = newUserPermission.Permission;
                        newUserPermissionsCopy.Remove(newUserPermission);
                    }
                }
            }
            // Add new permissions for users
            foreach (var newUserPermission in newUserPermissionsCopy)
            {
                subFolder.UserPermissions.Add(newUserPermission);
            }

            await UpdateSubFoldersPermissionsRecursively(subFolder, newCoursePermissionCopy, newUserPermissionsCopy);
        }
    }

    internal async Task DeleteFolder(long folderId)
    {
        var folder = await _context.Folders.FindAsync(folderId);
        if (folder == null) return;

        _context.Folders.Remove(folder);
        await _context.SaveChangesAsync();
    }

}
