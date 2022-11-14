using Concerto.Server.Extensions;
using Concerto.Server.Middlewares;
using Concerto.Server.Services;
using Concerto.Shared.Extensions;
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
        long userId = HttpContext.GetUserId();
        if (!await _storageService.CanReadInFolder(userId, folderId)) return Forbid();
        return Ok(await _storageService.GetFolderContent(folderId, userId));
    }

    [HttpGet]
    public async Task<ActionResult<Dto.FolderSettings>> GetFolderSettings([FromQuery] long folderId)
    {
        long userId = HttpContext.GetUserId();
        if (!await _storageService.CanWriteInFolder(userId, folderId)) return Forbid();
        return Ok(await _storageService.GetFolderSettings(folderId));
    }
    
    [HttpDelete]
    public async Task<ActionResult> DeleteFolder([FromQuery] long folderId)
    {
        long userId = HttpContext.GetUserId();
        // Check if user can delete folder
        if(!await _storageService.CanDeleteFolder(userId, folderId)) return Forbid();
        // Delete folder
        await _storageService.DeleteFolder(folderId);
        return NoContent();
    }

    [Authorize(Roles = "teacher")]
    [HttpPost]
    public async Task<ActionResult> CreateFolder([FromBody] Dto.CreateFolderRequest createFolderRequest)
    {
        long userId = HttpContext.GetUserId();
        if (!await _storageService.CanWriteInFolder(userId, createFolderRequest.ParentId)) return Forbid();
        await _storageService.CreateFolder(createFolderRequest, userId);
        return Ok();
    }

    [Authorize(Roles = "teacher")]
    [HttpPost]
    public async Task<ActionResult> UpdateFolder([FromBody] Dto.UpdateFolderRequest updateFolderRequest)
    {
        long userId = HttpContext.GetUserId();
        if (!await _storageService.CanDeleteFolder(userId, updateFolderRequest.Id)) return Forbid();
        await _storageService.UpdateFolder(updateFolderRequest);
        return Ok();
    }

    [Authorize(Roles = "teacher")]
    [HttpPost]
    public async Task<ActionResult<IEnumerable<Dto.FileUploadResult>>> UploadFiles([FromForm] IEnumerable<IFormFile> files, [FromQuery] long folderId)
    {
        long userId = HttpContext.GetUserId();
        if (!await _storageService.CanWriteInFolder(userId, folderId)) return Forbid();

        var fileUploadResults = await _storageService.AddFilesToFolder(files, folderId);
        return Ok(fileUploadResults);
    }

    [HttpGet]
    public async Task<ActionResult> DownloadFile([FromQuery] long fileId)
    {
        long userId = HttpContext.GetUserId();
        if (!await _storageService.HasFileReadAccess(userId, fileId)) return Forbid();

        var file = await _storageService.GetFile(fileId);
        if (file == null) return NotFound();

        byte[] fileBytes = System.IO.File.ReadAllBytes(file.Path);
        string fileName = file.DisplayName;
        return File(fileBytes, System.Net.Mime.MediaTypeNames.Application.Octet, fileName);
    }

}
