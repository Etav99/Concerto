using Concerto.Server.Extensions;
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
        long? userId = User.GetUserId();
        if (userId == null) return Unauthorized();

        return Ok(await _storageService.GetOwnedCatalogs(userId.Value));
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Dto.CatalogListItem>>> GetSharedCatalogs()
    {
        long? userId = User.GetUserId();
        if (userId == null) return Unauthorized();

        return Ok(await _storageService.GetSharedCatalogs(userId.Value));
    }

    [HttpGet]
    public async Task<ActionResult<Dto.CatalogContent>> GetCatalogContent([FromQuery] long catalogId)
    {
        long? userId = User.GetUserId();
        if (userId == null) return Unauthorized();

        if (!await _storageService.HasCatalogReadAccess(userId.Value, catalogId)) return Unauthorized();

        return Ok(await _storageService.GetCatalogContent(catalogId));
    }

    [HttpGet]
    public async Task<ActionResult<Dto.CatalogSettings>> GetCatalogSettings([FromQuery] long catalogId)
    {
        long? userId = User.GetUserId();
        if (userId == null) return Unauthorized();

        if (!await _storageService.HasCatalogReadAccess(userId.Value, catalogId)) return Unauthorized();

        return Ok(await _storageService.GetCatalogSettings(catalogId));
    }

    [HttpPost]
    public async Task<ActionResult> CreateCatalog([FromBody] Dto.CreateCatalogRequest createCatalogRequest)
    {
        long? userId = User.GetUserId();
        if (userId == null) return Unauthorized();

        await _storageService.CreateCatalog(createCatalogRequest, userId.Value);
        return Ok();
    }

    [HttpPost]
    public async Task<ActionResult> UpdateCatalog([FromBody] Dto.UpdateCatalogRequest updateCatalogRequest)
    {
        long? userId = User.GetUserId();
        if (userId == null) return Unauthorized();

        if (!await _storageService.HasCatalogWriteAccess(User.GetUserId(), updateCatalogRequest.Id)) return Unauthorized();
        await _storageService.UpdateCatalog(updateCatalogRequest);
        return Ok();
    }


    [HttpGet]
    public async Task<ActionResult<IEnumerable<Dto.CatalogListItem>>> GetSessionCatalogs([FromQuery] long sessionId)
    {
        long? userId = User.GetUserId();
        if (userId == null) return Unauthorized();

        if (!await _sessionService.IsUserSessionMember(userId.Value, sessionId)) return Unauthorized();
        var catalogs = await _storageService.GetSessionCatalogs(userId.Value, sessionId);
        return Ok(catalogs);
    }

    [HttpPost]
    public async Task<ActionResult<IEnumerable<Dto.FileUploadResult>>> UploadFiles([FromForm] IEnumerable<IFormFile> files, [FromQuery] long catalogId)
    {
        // Check if user has write access to catalog
        if (!await _storageService.HasCatalogWriteAccess(User.GetUserId(), catalogId)) return Unauthorized();

        var fileUploadResults = await _storageService.AddFilesToCatalog(files, catalogId);
        return Ok(fileUploadResults);
    }

    [HttpGet]
    public async Task<ActionResult> DownloadFile([FromQuery] long fileId)
    {
        // Check if user has read access to file
        if (!await _storageService.HasFileReadAccess(User.GetUserId(), fileId)) return Unauthorized();

        var file = await _storageService.GetFile(fileId);
        if (file == null) return NotFound();

        byte[] fileBytes = System.IO.File.ReadAllBytes(file.Path);
        string fileName = file.DisplayName;
        return File(fileBytes, System.Net.Mime.MediaTypeNames.Application.Octet, fileName);
    }

}
