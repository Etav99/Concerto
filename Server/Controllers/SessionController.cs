using Concerto.Server.Middlewares;
using Concerto.Server.Services;
using Concerto.Server.Settings;
using Concerto.Shared.Extensions;
using Concerto.Shared.Models.Dto;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Concerto.Server.Controllers;

[Route("[controller]/[action]")]
[ApiController]
[Authorize]
public class SessionController : ControllerBase
{
	private readonly WorkspaceService _workspaceService;
	private readonly ILogger<SessionController> _logger;
	private readonly SessionService _sessionService;
	private readonly StorageService _storageService;
	private Guid UserId => HttpContext.UserId();


	public SessionController(ILogger<SessionController> logger, WorkspaceService workspaceService, SessionService sessionService, StorageService storageService)
	{
		_logger = logger;
		_workspaceService = workspaceService;
		_sessionService = sessionService;
		_storageService = storageService;
	}

	[HttpPost]
	public async Task<ActionResult<long>> CreateSession([FromBody] CreateSessionRequest request)
	{
		if (!User.IsAdmin() && !await _workspaceService.CanManageWorkspaceSessions(request.WorkspaceId, UserId)) return Forbid();

		var sessionId = await _sessionService.CreateSession(request, UserId);
		if (sessionId is not null) return Ok(sessionId);
		return BadRequest();
	}

	[HttpGet]
	public async Task<ActionResult<Session>> GetSession(long sessionId)
	{
		var isAdmin = User.IsAdmin();
		if (!isAdmin && !await _sessionService.CanAccessSession(sessionId, UserId)) return Forbid();

		var session = await _sessionService.GetSession(sessionId, UserId, isAdmin);
		return session is null ? NotFound() : Ok(session);
	}

	[HttpDelete]
	public async Task<ActionResult> DeleteSession(long sessionId)
	{
		if (!await _sessionService.CanManageSession(sessionId, UserId)) return Forbid();
		if (!await _sessionService.DeleteSession(sessionId)) return Forbid();
		return Ok();
	}

	[HttpGet]
	public async Task<ActionResult<IEnumerable<SessionListItem>>> GetWorkspaceSessions(long workspaceId)
	{
		if (!User.IsAdmin() && !await _workspaceService.IsUserWorkspaceMember(UserId, workspaceId)) return Forbid();
		return Ok(await _sessionService.GetWorkspaceSessions(workspaceId));
	}

	[HttpGet]
	public async Task<ActionResult<SessionSettings>> GetSessionSettings(long sessionId)
	{
		if (!User.IsAdmin() && !await _sessionService.CanManageSession(sessionId, UserId)) return Forbid();

		var sessionSettings = await _sessionService.GetSessionSettings(sessionId);
		if (sessionSettings == null) return NotFound();
		return Ok(sessionSettings);
	}

	[HttpPost]
	public async Task<ActionResult> UpdateSession(UpdateSessionRequest request)
	{
		if (!User.IsAdmin() && !await _sessionService.CanManageSession(request.SessionId, UserId)) return Forbid();
		if (!await _sessionService.UpdateSession(request)) return Forbid();

		return Ok();
	}

	[HttpGet]
	[Produces("text/plain")]
	public async Task<ActionResult<string>> GetMeetingToken(Guid meetingGuid)
	{
		if (!User.IsAdmin() && !await _sessionService.CanAccessSession(meetingGuid, UserId)) return Forbid();
		return await _sessionService.GenerateMeetingToken(UserId, meetingGuid);
	}


	[HttpPost]
	[AllowAnonymous]
	[ProducesResponseType(StatusCodes.Status200OK)]
	[ProducesResponseType(StatusCodes.Status401Unauthorized)]
	[ProducesResponseType(StatusCodes.Status400BadRequest)]
	public async Task<ActionResult<Guid>> RecordingFinished([FromBody] RecordingFinishedRequest request)
	{
		if(request.RecorderKey != AppSettings.Meetings.RecorderKey) return Unauthorized();

		try
		{
			await _storageService.SaveRecording(request.MeetingId, request.FilePath);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error while saving recording");
			return BadRequest();
		}
		return Ok();
	}
}

