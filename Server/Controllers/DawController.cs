using Concerto.Server.Hubs;
using Concerto.Server.Middlewares;
using Concerto.Server.Services;
using Concerto.Shared.Constants;
using Concerto.Shared.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Concerto.Server.Controllers;

[Route("[controller]/[action]")]
[ApiController]
public class DawController : ControllerBase
{
	private readonly ILogger<DawController> _logger;
	private readonly DawService _dawService;
	private readonly SessionService _sessionService;

	private Guid UserId => HttpContext.UserId();

    public DawController(ILogger<DawController> logger, DawService dawService, SessionService sessionService)
    {
        _logger = logger;
        _dawService = dawService;
        _sessionService = sessionService;
    }

    [HttpGet]
	public async Task<ActionResult<Dto.DawProject>> GetProject(long sessionId)
	{
		if(!await _sessionService.CanAccessSession(sessionId, UserId)) return Forbid();
		var project = await _dawService.GetProject(sessionId, UserId);
		if (project == null) return NotFound();
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
	public async Task AddTrack(long projectId, string trackName)
	{
        await _dawService.AddTrack(projectId, trackName);
    }

	[HttpPost]
	public async Task SetTrackStartTime(long projectId, long trackId, float startTime)
	{
		await _dawService.SetTrackStartTime(projectId, trackId, startTime);
	}

	[HttpPost]
	public async Task SetTrackVolume(long projectId, long trackId, float volume)
	{
		await _dawService.SetTrackVolume(projectId, trackId, volume);
	}

	[HttpPost]
	public async Task SelectTrack(long projectId, long trackId)
	{
		await _dawService.SelectTrack(projectId, trackId, UserId);
	}

	[HttpPost]
	public async Task UnselectTrack(long projectId, long trackId)
	{
		await _dawService.UnselectTrack(projectId, trackId, UserId);
    }

	[HttpPost]
	[RequestFormLimits(MemoryBufferThreshold = 1024 * 1024 * 1024)]
	public async Task<IActionResult> SetTrackSource([FromForm] long projectId, [FromForm]long trackId, [FromForm] IFormFile file)
	{
		await _dawService.SetTrackSource(projectId, trackId, file);
		return Ok();
	}

	[HttpGet]
	public async Task<IActionResult> GetTrackSource([FromQuery] long projectId, [FromQuery] long trackId)
	{
		var fileStream = await _dawService.GetTrackSourceStream(projectId, trackId);
        return File(fileStream, "audio/*");
    }

	[HttpGet]
	public async Task<IActionResult> GetProjectCombinedSource([FromQuery] long projectId)
	{
		var fileStream = await _dawService.GetProjectSourceStream(projectId);
        return File(fileStream, "audio/*");
    }

	[HttpPost]
	public async Task<IActionResult> GenerateProjectSource(long projectId)
	{
		await _dawService.GenerateProjectSource(projectId);
		return Ok();
    }

}