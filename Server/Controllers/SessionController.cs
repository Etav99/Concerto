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
public class SessionController : ControllerBase
{
	private readonly CourseService _courseService;
	private readonly ILogger<SessionController> _logger;
	private readonly SessionService _sessionService;
	private Guid UserId => HttpContext.UserId();


	public SessionController(ILogger<SessionController> logger, CourseService courseService, SessionService sessionService)
	{
		_logger = logger;
		_courseService = courseService;
		_sessionService = sessionService;
	}

	[HttpPost]
	public async Task<ActionResult<long>> CreateSession([FromBody] CreateSessionRequest request)
	{
		if (!User.IsAdmin() && !await _courseService.CanManageCourseSessions(request.CourseId, UserId)) return Forbid();

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
	public async Task<ActionResult<IEnumerable<SessionListItem>>> GetCourseSessions(long courseId)
	{
		if (!User.IsAdmin() && !await _courseService.IsUserCourseMember(UserId, courseId)) return Forbid();
		return Ok(await _sessionService.GetCourseSessions(courseId));
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
}

