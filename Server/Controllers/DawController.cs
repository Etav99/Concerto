using Concerto.Server.Data.Models.Daw;
using Concerto.Server.Hubs;
using Concerto.Server.Middlewares;
using Concerto.Server.Services;
using Concerto.Shared.Constants;
using Concerto.Shared.Extensions;
using Concerto.Shared.Models.Dto;
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

	public DawController(ILogger<DawController> logger, DawService dawService)
	{
		_logger = logger;
		_dawService = dawService;
	}

	[HttpGet]
	public bool CanModifyTrack(long sessionId, string trackName)
		=> _dawService.CanModifyTrack(UserId, sessionId, trackName);

	[HttpGet]
	public Dto.DawProject GetProject(long sessionId)
		=> _dawService.GetProject(sessionId, UserId);

	[HttpGet]
	public Dto.Track GetTrack(long sessionId, string trackName)
	{
		return _dawService.GetTrack(sessionId, trackName, UserId);
	}

	[HttpDelete]
	public async Task DeleteTrack(long sessionId, string trackName)
	{
		await _dawService.DeleteTrack(sessionId, trackName);
	}

	[HttpPost]
	public async Task AddTrack(long sessionId, string trackName)
	{
        await _dawService.AddTrack(sessionId, trackName);
    }

	[HttpPost]
	public async Task SetTrackStartTime(long sessionId, string trackName, float startTime)
	{
		await _dawService.SetTrackStartTime(sessionId, trackName, startTime);
	}

	[HttpPost]
	public async Task SetTrackVolume(long sessionId, string trackName, float volume)
	{
		await _dawService.SetTrackVolume(sessionId, trackName, volume);
	}

	[HttpPost]
	public async Task SelectTrack(long sessionId, string trackName)
	{
		await _dawService.SelectTrack(sessionId, trackName, UserId);
	}

	[HttpPost]
	public async Task UnselectTrack(long sessionId, string trackName)
	{
		await _dawService.UnselectTrack(sessionId, trackName);
    }

	[HttpPost]
	[RequestFormLimits(MemoryBufferThreshold = 1024 * 1024 * 1024)]
	public async Task<IActionResult> SetTrackSource([FromForm] long sessionId, [FromForm]string trackName, [FromForm] IFormFile file)
	{
		await _dawService.SetTrackSource(sessionId, trackName, file);

		return Ok();
	}

	[HttpGet]
	public async Task<IActionResult> GetTrackSource([FromQuery] long sessionId,[FromQuery] string trackName)
	{
        var file = _dawService.GetTrackSource(sessionId, trackName);
		// TODO
        return File(file, "audio/*");
    }


}