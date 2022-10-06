using Concerto.Server.Data.DatabaseContext;
using Concerto.Server.Data.Models;
using Concerto.Server.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Net;

namespace Concerto.Server.Services;

public class FileService
{
    private readonly ILogger<FileService> _logger;

    private readonly AppDataContext _context;
    public FileService(ILogger<FileService> logger, AppDataContext context)
    {
        _logger = logger;
        _context = context;
    }

    public async Task<UploadedFile?> GetSessionFile(long fileId)
    {
        return await _context.UploadedFiles.FindAsync(fileId);
    }

    public async Task<IEnumerable<Dto.UploadedFile>> GetSessionFiles(long sessionId)
    {
        return await _context.UploadedFiles
            .Where(f => f.SessionId == sessionId)
            .Select(f => f.ToDto())
            .ToListAsync();
    }

    public async Task<IEnumerable<FileUploadResult>> UploadFiles([FromForm] IEnumerable<IFormFile> files)
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
                        string path = Path.Combine("UserFileStorage", storageFileName);
                        Directory.CreateDirectory(Path.Combine("UserFileStorage"));
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
}
