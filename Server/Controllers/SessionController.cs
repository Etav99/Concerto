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
public class SessionController : ControllerBase
{
	private readonly ILogger<SessionController> _logger;
	private readonly CourseService _courseService;
	private readonly SessionService _sessionService;
	private long UserId => HttpContext.UserId();

	public SessionController(ILogger<SessionController> logger, CourseService courseService, SessionService sessionService)
	{
		_logger = logger;
		_courseService = courseService;
		_sessionService = sessionService;
	}

	[Authorize(Roles = "teacher")]
	[HttpPost]
	public async Task<ActionResult> CreateSession([FromBody] Dto.CreateSessionRequest request)
	{
		if (!await _courseService.IsUserCourseMember(UserId, request.CourseId)) return Forbid();

		if (await _sessionService.CreateSession(request))
		{
			return Ok();
		}
		return BadRequest();
	}

	[HttpGet]
	public async Task<ActionResult<Dto.Session>> GetSession(long sessionId)
	{
		bool isAdmin = User.IsAdmin();
		if (!isAdmin && !await _sessionService.CanAccessSession(UserId, sessionId)) return Forbid();

		var session = await _sessionService.GetSession(sessionId, UserId, isAdmin);
		return session is null ? NotFound() : Ok(session);
	}

	[HttpDelete]
	public async Task<ActionResult> DeleteSession(long sessionId)
	{
		if (!await _sessionService.CanManageSession(UserId, sessionId)) return Forbid();
		if (!await _sessionService.DeleteSession(sessionId)) return Forbid();
		return Ok();
	}

	[HttpGet]
	public async Task<ActionResult<IEnumerable<Dto.SessionListItem>>> GetCourseSessions(long courseId)
	{
		if (!User.IsAdmin() && !await _courseService.IsUserCourseMember(UserId, courseId)) return Forbid();
		return Ok(await _sessionService.GetCourseSessions(courseId));
	}

	[HttpGet]
	public async Task<ActionResult<Dto.SessionSettings>> GetSessionSettings(long sessionId)
	{
		if (!User.IsAdmin() && !await _sessionService.CanManageSession(sessionId, UserId)) return Forbid();
		
		var sessionSettings = await _sessionService.GetSessionSettings(sessionId);
		if (sessionSettings == null) return NotFound();
		return Ok(sessionSettings);
	}

	[HttpPost]
	public async Task<ActionResult> UpdateSession(Dto.UpdateSessionRequest request)
	{
		if (!User.IsAdmin() && !await _sessionService.CanManageSession(request.SessionId, UserId)) return Forbid();
		if (!await _sessionService.UpdateSession(request)) return Forbid();
		
		return Ok();
	}

}
