using Concerto.Server.Data.DatabaseContext;
using Concerto.Server.Data.Models;
using Concerto.Server.Extensions;
using Microsoft.AspNetCore.Mvc;
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

    public async Task<UploadedFile?> GetFile(long fileId)
    {
        return await _context.UploadedFiles.FindAsync(fileId);
    }

    public async Task<IEnumerable<Dto.UploadedFile>> GetCatalogFiles(long catalogId)
    {
        return await _context.UploadedFiles
            .Where(f => f.CatalogId == catalogId)
            .Select(f => f.ToDto())
            .ToListAsync();
    }

    public async Task<IEnumerable<Dto.FileUploadResult>> AddFilesToCatalog(IEnumerable<IFormFile> files, long catalogId)
    {
        var fileUploadResults = await SaveUploadedFiles(files, catalogId);

        foreach (var fileUploadResult in fileUploadResults)
        {
            if (fileUploadResult.Uploaded && !string.IsNullOrEmpty(fileUploadResult.DisplayFileName) && !string.IsNullOrEmpty(fileUploadResult.StorageFileName))
            {
                var uploadedFile = new UploadedFile()
                {
                    DisplayName = fileUploadResult.DisplayFileName,
                    StorageName = fileUploadResult.StorageFileName,
                    CatalogId = catalogId
                };
                _context.Add(uploadedFile);
            }
        }

        _context.SaveChanges();
        return fileUploadResults.ToDto();
    }

    public async Task<IEnumerable<FileUploadResult>> SaveUploadedFiles(IEnumerable<IFormFile> files, long catalogId)
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
                        string storageFileName = Path.GetRandomFileName();
                        fileUploadResult.StorageFileName = storageFileName;
                        string path = Path.Combine("/var/lib/concerto/storage", $"{catalogId}", storageFileName);
                        Directory.CreateDirectory(Path.Combine("/var/lib/concerto/storage", $"{catalogId}"));
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

    internal async Task<bool> HasFileReadAccess(long? userId, long fileId)
    {
        if (userId == null) return false;
        var file = await _context.UploadedFiles.FindAsync(fileId);
        if (file == null) return false;
        return await HasCatalogReadAccess(userId, file.CatalogId);
    }
    
    internal async Task<bool> HasCatalogReadAccess(long? userId, long catalogId)
    {
        if (userId == null) return false;
        var catalog = await _context.Catalogs.FindAsync(catalogId);
        if (catalog == null) return false;
        if (catalog.OwnerId == userId) return true;
        
        await _context.Entry(catalog).Collection(c => c.UsersSharedTo).LoadAsync();
        return catalog.UsersSharedTo.Any(u => u.Id == userId);
    }

    internal async Task<bool> HasCatalogWriteAccess(long? userId, long catalogId)
    {
        if (userId == null) return false;
        var catalog = await _context.Catalogs.FindAsync(catalogId);
        if (catalog == null) return false;
        if (catalog.OwnerId != userId) return false;
        return true;
    }
    
    internal async Task<IEnumerable<Dto.Catalog>> GetUserCatalogs(long userId)
    {
        return await _context.Catalogs
            .Where(c => c.OwnerId == userId)
            .Select(c => c.ToDto(true, false))
            .ToListAsync();
    }

    internal async Task<IEnumerable<Dto.Catalog>> GetSharedCatalogs(long userId)
    {
        return await _context.Catalogs
            .Include(c => c.UsersSharedTo)
            .Where(c => c.UsersSharedTo.Any(u => u.Id == userId))
            .Select(c => c.ToDto(false, false))
            .ToListAsync();
    }

    internal async Task<IEnumerable<Dto.Catalog>> GetSessionCatalogs(long userId, long sessionId)
    {
        return await _context.Catalogs
            .Include(c => c.SharedInSessions)
            .Where(c => c.SharedInSessions.Any(s => s.Id == sessionId))
            .Select(c => c.ToDto(c.OwnerId == userId, false))
            .ToListAsync();
    }
    
    internal async Task CreateCatalog(Dto.CreateCatalogRequest createCatalogRequest, long ownerId)
    {
        var sharedToSessions = await _context.Sessions
            .Where(s => createCatalogRequest.SharedToSessionIds.Contains(s.Id))
            .ToListAsync();
        await _context.Catalogs.AddAsync(new Catalog()
        {
            Name = createCatalogRequest.Name,
            OwnerId = ownerId,
            SharedInSessions = sharedToSessions
        });
        await _context.SaveChangesAsync();
    }
}
