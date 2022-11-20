using Concerto.Server.Data.Models;
using Concerto.Server.Extensions;
using Concerto.Server.Middlewares;
using Concerto.Server.Services;
using Concerto.Shared.Extensions;
using Concerto.Shared.Models.Dto;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Concerto.Server.Controllers;

[Route("[controller]/[action]")]
[ApiController]
[Authorize]
public class StorageController : ControllerBase
{
    private readonly ILogger<StorageController> _logger;
    private readonly StorageService _storageService;
    private readonly SessionService _sessionService;

    public StorageController(ILogger<StorageController> logger, StorageService storageService, SessionService sessionService)
    {
        _logger = logger;
        _storageService = storageService;
        _sessionService = sessionService;
    }


    [HttpGet]
    public async Task<ActionResult<Dto.FolderContent>> GetFolderContent([FromQuery] long folderId)
    {
        long userId = HttpContext.UserId();
        if (User.IsInRole("admin"))
        {
            return Ok(await _storageService.GetFolderContent(folderId, userId, true));
        }
        
        if (!await _storageService.CanReadInFolder(userId, folderId)) return Forbid();
        return Ok(await _storageService.GetFolderContent(folderId, userId));
    }

    [HttpGet]
    public async Task<ActionResult<Dto.FolderSettings>> GetFolderSettings([FromQuery] long folderId)
    {
        long userId = HttpContext.UserId();
        if (User.IsInRole("admin") || await _storageService.CanWriteInFolder(userId, folderId))
        {
            return Ok(await _storageService.GetFolderSettings(folderId));
        }
        return Forbid();
    }
    
    [HttpDelete]
    public async Task<ActionResult> DeleteFolder([FromQuery] long folderId)
    {
        long userId = HttpContext.UserId();
        if (User.IsInRole("admin") || await _storageService.CanDeleteFolder(userId, folderId))
        {
            await _storageService.DeleteFolder(folderId);
            return Ok();
        }
         return Forbid();
    }

    [HttpPost]
    public async Task<ActionResult> CreateFolder([FromBody] Dto.CreateFolderRequest createFolderRequest)
    {
        long userId = HttpContext.UserId();
        if(User.IsInRole("admin") || await _storageService.CanWriteInFolder(userId, createFolderRequest.ParentId))
        {
            await _storageService.CreateFolder(createFolderRequest, userId);
            return Ok();
        }
        return Forbid();
    }

    [HttpPost]
    public async Task<ActionResult> UpdateFolder([FromBody] Dto.UpdateFolderRequest updateFolderRequest)
    {
        long userId = HttpContext.UserId();
        if (User.IsInRole("admin") || await _storageService.CanEditFolder(userId, updateFolderRequest.Id))
        {
            await _storageService.UpdateFolder(updateFolderRequest);
            return Ok();
        }
        return Forbid();
    }

    [HttpPost]
    public async Task<ActionResult<IEnumerable<Dto.FileUploadResult>>> UploadFiles([FromForm] IEnumerable<IFormFile> files, [FromQuery] long folderId)
    {
        long userId = HttpContext.UserId();
        if (User.IsInRole("admin") || await _storageService.CanWriteInFolder(userId, folderId))
        {
            var fileUploadResults = await _storageService.AddFilesToFolder(files, folderId);
            return Ok(fileUploadResults);
        }
        return Forbid();
    }

	[HttpDelete]
	public async Task<ActionResult> DeleteFile([FromQuery] long fileId)
	{
		long userId = HttpContext.UserId();
        if (User.IsInRole("admin") || await _storageService.CanManageFile(userId, fileId))
        {
            await _storageService.DeleteFile(fileId);
            return Ok();
        }
        return Forbid();
	}

	[HttpGet]
    public async Task<ActionResult> DownloadFile([FromQuery] long fileId)
    {
        long userId = HttpContext.UserId();
        if (User.IsInRole("admin") || await _storageService.HasFileReadAccess(userId, fileId))
        {
            var file = await _storageService.GetFile(fileId);
            if (file == null) return NotFound();
            byte[] fileBytes = System.IO.File.ReadAllBytes(file.Path);
            string fileName = file.DisplayName;
            return File(fileBytes, System.Net.Mime.MediaTypeNames.Application.Octet, fileName);
        }
        return Forbid();
    }

}
