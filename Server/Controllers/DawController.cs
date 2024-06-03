using Concerto.Server.Hubs;
using Concerto.Server.Middlewares;
using Concerto.Server.Services;
using Concerto.Shared.Constants;
using Concerto.Shared.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;
using Newtonsoft.Json.Linq;

namespace Concerto.Server.Controllers;

[Route("[controller]/[action]")]
[ApiController]
public class DawController : ControllerBase
{
	private readonly ILogger<DawController> _logger;
	private readonly DawService _dawService;
	private readonly SessionService _sessionService;
	private readonly StorageService _storageService;
	private readonly TokenStore _tokenStore;

	private Guid UserId => HttpContext.UserId();

	public DawController(ILogger<DawController> logger, DawService dawService, SessionService sessionService, StorageService storageService, TokenStore tokenStore)
	{
		_logger = logger;
		_dawService = dawService;
		_sessionService = sessionService;
		_storageService = storageService;
		_tokenStore = tokenStore;
	}

	[HttpGet]
	public async Task<ActionResult<Dto.DawProject>> GetProject(long sessionId)
	{
		if(!await _sessionService.CanAccessSession(sessionId, UserId)) return Forbid();
		var project = await _dawService.GetProject(sessionId, UserId);
		if (project == null) return NotFound();

		project.Token = _tokenStore.GenerateToken(sessionId, TokenStore.TokenType.DawProject);
		return project;
	}

	[HttpGet]
	public async Task<ActionResult<Dto.Track>> GetTrack(long projectId, long trackId)
	{
		if(!await _sessionService.CanAccessSession(projectId, UserId)) return Forbid();
		var track = await _dawService.GetTrack(projectId, trackId, UserId);
		if (track == null) return NotFound();
		return track;
	}

	[HttpDelete]
	public async Task<ActionResult> DeleteTrack(long projectId, long trackId)
	{
		if(!await _sessionService.CanAccessSession(projectId, UserId)) return Forbid();
		await _dawService.DeleteTrack(projectId, trackId);
		return Ok();
	}

	[HttpPost]
	public async Task<IActionResult> AddTrack(long projectId, string? trackName)
	{
		if(!await _sessionService.CanAccessSession(projectId, UserId)) return Forbid();
		if (trackName == null) trackName = string.Empty;
        await _dawService.AddTrack(projectId, trackName);
		return Ok();
    }

	[HttpPost]
	public async Task<IActionResult> SetTrackStartTime(long projectId, long trackId, float startTime)
	{
		if(!await _sessionService.CanAccessSession(projectId, UserId)) return Forbid();
		await _dawService.SetTrackStartTime(projectId, trackId, startTime);
		return Ok();
	}

	[HttpPost]
	public async Task<IActionResult> SetTrackVolume(long projectId, long trackId, float volume)
	{
		if(!await _sessionService.CanAccessSession(projectId, UserId)) return Forbid();
		await _dawService.SetTrackVolume(projectId, trackId, volume);
		return Ok();
			
	}

	[HttpPost]
	public async Task<IActionResult> SelectTrack(long projectId, long trackId)
	{
		if(!await _sessionService.CanAccessSession(projectId, UserId)) return Forbid();
		await _dawService.SelectTrack(projectId, trackId, UserId);
		return Ok();
	}

	[HttpPost]
	public async Task<IActionResult> UnselectTrack(long projectId, long trackId)
	{
		if(!await _sessionService.CanAccessSession(projectId, UserId)) return Forbid();
		await _dawService.UnselectTrack(projectId, trackId, UserId);
		return Ok();
    }

	[HttpPost]
	[RequestFormLimits(MemoryBufferThreshold = 1024 * 1024 * 1024)]
	public async Task<IActionResult> SetTrackSource([FromForm] long projectId, [FromForm]long trackId, [FromForm] IFormFile file, [FromForm] float? startTime, [FromForm] float? volume)
	{
		if(!await _sessionService.CanAccessSession(projectId, UserId)) return Forbid();

		await _dawService.SetTrackSource(projectId, trackId, file, startTime, volume);
		return Ok();
	}


	[HttpPost]
	public async Task<IActionResult> SetTrackName(long projectId, long trackId, string? name)
	{
		if(name is null) name = string.Empty;
		if(!await _sessionService.CanAccessSession(projectId, UserId)) return Forbid();
		await _dawService.SetTrackName(projectId, trackId, name, UserId);
		return Ok();
    }

	[HttpGet]
	[AllowAnonymous]
	public async Task<IActionResult> GetTrackSource([FromQuery] long projectId, [FromQuery] long trackId, [FromQuery] Guid token)
	{
		if(!_tokenStore.ValidateToken(token, projectId, TokenStore.TokenType.DawProject)) return Forbid();

		var fileStream = await _dawService.GetTrackSourceStream(projectId, trackId);
        return File(fileStream, "audio/*", "Track", true);
    }

	[HttpGet]
	[AllowAnonymous]
	public async Task<IActionResult> GetProjectSource([FromQuery] long projectId, [FromQuery] Guid token)
	{
		if(!_tokenStore.ValidateToken(token, projectId, TokenStore.TokenType.DawProject)) return Forbid();

		var (fileStream, hash) = await _dawService.GetProjectSourceStream(projectId);

		// Set no cache
		var headers = Response.GetTypedHeaders();
		headers.CacheControl = new CacheControlHeaderValue()
		{
			NoCache = true,
			NoStore = true,
			MustRevalidate = true
		};
		Response.Headers[HeaderNames.Pragma] = "no-cache";
		Response.Headers[HeaderNames.Expires] = "0";

		var tag = new EntityTagHeaderValue($"\"{hash}\"");

        return File(fileStream: fileStream, lastModified: null, contentType: "audio/mp3", entityTag: tag, enableRangeProcessing: true);
    }

	[HttpPost]
	public async Task<ActionResult<bool>> GenerateProjectSource(long projectId)
	{
		if(!await _sessionService.CanAccessSession(projectId, UserId)) return Forbid();
		return await _dawService.GenerateProjectSource(projectId);
    }

	[HttpPost]
	public async Task<IActionResult> SaveProjectSource(long projectId, long destinationFolder, string filename)
	{
		if(!await _sessionService.CanAccessSession(projectId, UserId)) return Forbid();
		if(!await _storageService.CanWriteInFolder(UserId, destinationFolder)) return Forbid();

		await _dawService.SaveProjectSource(projectId, destinationFolder, filename, UserId);
		return Ok();
    }

	[HttpGet]
	public async Task<ActionResult<Guid>> GetProjectToken(long projectId)
	{
		if(!await _sessionService.CanAccessSession(projectId, UserId)) return Forbid();
		var token = _tokenStore.GenerateToken(projectId, TokenStore.TokenType.DawProject);
		return Ok(token);
	}
}