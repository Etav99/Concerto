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
        long userId = HttpContext.UserId();
		if (!await _courseService.IsUserCourseMember(userId, request.CourseId)) return Forbid();

		if (await _sessionService.CreateSession(request))
		{
			return Ok();
		}
		return BadRequest();
	}

	[HttpGet]
	public async Task<ActionResult<Dto.Session>> GetSession(long sessionId)
	{
        long userId = HttpContext.UserId();
		if (!await _sessionService.CanAccessSession(userId, sessionId)) return Forbid();
		var session = await _sessionService.GetSession(sessionId);
        return session is null ? NotFound() : Ok(session);
    }
    
	[HttpGet]
	public async Task<ActionResult<IEnumerable<Dto.SessionListItem>>> GetCourseSessions(long courseId)
	{
        long userId = HttpContext.UserId();
        if (!await _courseService.IsUserCourseMember(userId, courseId)) return Forbid();
        return Ok(await _sessionService.GetCourseSessions(courseId));
	}

}
