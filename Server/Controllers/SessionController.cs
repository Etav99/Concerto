using Concerto.Server.Extensions;
using Concerto.Server.Services;
using Concerto.Shared.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Concerto.Server.Controllers;

[Route("[controller]/[action]")]
[ApiController]
[Authorize]
public class SessionController : ControllerBase
{
    private readonly ILogger<SessionController> _logger;
    private readonly FileService _fileService;
    private readonly RoomService _roomService;
    private readonly SessionService _sessionService;


    public SessionController(ILogger<SessionController> logger, FileService fileService, RoomService roomService, SessionService sessionService)
    {
        _logger = logger;
        _fileService = fileService;
        _roomService = roomService;
        _sessionService = sessionService;
    }

    [HttpPost]
    public async Task<ActionResult> CreateSession([FromBody] Dto.CreateSessionRequest request)
    {
        long? userId = User.GetUserId();
        if (userId == null) return Unauthorized();
        if (!await _roomService.IsUserRoomMember(userId.Value, request.RoomId)) return Unauthorized();

        if (await _sessionService.CreateSession(request))
        {
            return Ok();
        }
        return BadRequest();
    }

    [HttpGet]
    public async Task<Dto.Session?> GetSession(long sessionId)
    {
        long? userId = User.GetUserId();
        if (userId == null) return null;
        if (!await _sessionService.IsUserSessionMember(userId.Value, sessionId)) return null;

        var session = await _sessionService.GetSession(sessionId);
        return session;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Dto.UploadedFile>>> GetSessionFiles(long sessionId)
    {
        long? userId = User.GetUserId();
        if (userId == null) return Unauthorized();

        if (!await _sessionService.IsUserSessionMember(userId.Value, sessionId)) return Unauthorized();

        return Ok(await _fileService.GetSessionFiles(sessionId));
    }

    [HttpPost]
    public async Task<ActionResult<IEnumerable<Dto.FileUploadResult>>> UploadSessionFiles([FromForm] IEnumerable<IFormFile> files, [FromQuery] long sessionId)
    {
        long? userId = User.GetUserId();
        if (userId == null || !await _sessionService.IsUserSessionMember(userId.Value, sessionId)) return Unauthorized();

        var fileUploadResults = await _sessionService.UploadSessionFiles(files, sessionId);
        return Ok(fileUploadResults);
    }

    [HttpGet]
    public async Task<ActionResult> DownloadSessionFile([FromQuery] long fileId)
    {
        var file = await _fileService.GetSessionFile(fileId);
        if (file == null) return NotFound();

        long? userId = User.GetUserId();
        if (userId == null || !await _sessionService.IsUserSessionMember(userId.Value, file.SessionId)) return Unauthorized();

        byte[] fileBytes = System.IO.File.ReadAllBytes(file.Path);
        string fileName = file.DisplayName;
        return File(fileBytes, System.Net.Mime.MediaTypeNames.Application.Octet, fileName);
    }
    
}
