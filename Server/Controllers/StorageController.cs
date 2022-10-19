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
    public async Task<ActionResult<IEnumerable<Dto.CatalogListItem>>> GetOwnedCatalogs()
    {
        long userId = HttpContext.GetUserId();
        return Ok(await _storageService.GetOwnedCatalogs(userId));
    }
    
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Dto.CatalogListItem>>> GetSharedCatalogs()
    {
        long userId = HttpContext.GetUserId();
        return Ok(await _storageService.GetSharedCatalogs(userId));
    }

    [HttpGet]
    public async Task<ActionResult<Dto.CatalogContent>> GetCatalogContent([FromQuery] long catalogId)
    {
        long userId = HttpContext.GetUserId();
        if (!await _storageService.HasCatalogReadAccess(userId, catalogId)) return Forbid();
        return Ok(await _storageService.GetCatalogContent(catalogId));
    }

    [HttpGet]
    public async Task<ActionResult<Dto.CatalogSettings>> GetCatalogSettings([FromQuery] long catalogId)
    {
        long userId = HttpContext.GetUserId();
        if (!await _storageService.HasCatalogWriteAccess(userId, catalogId)) return Forbid();
        return Ok(await _storageService.GetCatalogSettings(catalogId));
    }

    [Authorize(Roles = "teacher")]
    [HttpPost]
    public async Task<ActionResult> CreateCatalog([FromBody] Dto.CreateCatalogRequest createCatalogRequest)
    {
        long userId = HttpContext.GetUserId();
        await _storageService.CreateCatalog(createCatalogRequest, userId);
        return Ok();
    }

    [Authorize(Roles = "teacher")]
    [HttpPost]
    public async Task<ActionResult> UpdateCatalog([FromBody] Dto.UpdateCatalogRequest updateCatalogRequest)
    {
        long userId = HttpContext.GetUserId();
        if (!await _storageService.HasCatalogWriteAccess(userId, updateCatalogRequest.Id)) return Forbid();
        await _storageService.UpdateCatalog(updateCatalogRequest);
        return Ok();
    }


    [HttpGet]
    public async Task<ActionResult<IEnumerable<Dto.CatalogListItem>>> GetSessionCatalogs([FromQuery] long sessionId)
    {
        long userId = HttpContext.GetUserId();
        
        if (!await _sessionService.IsUserSessionMember(userId, sessionId)) return Forbid();
        var catalogs = await _storageService.GetSessionCatalogs(userId, sessionId);
        return Ok(catalogs);
    }

    [Authorize(Roles = "teacher")]
    [HttpPost]
    public async Task<ActionResult<IEnumerable<Dto.FileUploadResult>>> UploadFiles([FromForm] IEnumerable<IFormFile> files, [FromQuery] long catalogId)
    {
        long userId = HttpContext.GetUserId();
        if (!await _storageService.HasCatalogWriteAccess(userId, catalogId)) return Forbid();

        var fileUploadResults = await _storageService.AddFilesToCatalog(files, catalogId);
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
