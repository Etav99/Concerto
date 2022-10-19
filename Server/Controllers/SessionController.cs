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
	private readonly RoomService _roomService;
	private readonly SessionService _sessionService;


	public SessionController(ILogger<SessionController> logger, RoomService roomService, SessionService sessionService)
	{
		_logger = logger;
		_roomService = roomService;
		_sessionService = sessionService;
	}

    [Authorize(Roles = "teacher")]
    [HttpPost]
	public async Task<ActionResult> CreateSession([FromBody] Dto.CreateSessionRequest request)
	{
        long userId = HttpContext.GetUserId();
		if (!await _roomService.IsUserRoomMember(userId, request.RoomId)) return Forbid();

		if (await _sessionService.CreateSession(request))
		{
			return Ok();
		}
		return BadRequest();
	}

	[HttpGet]
	public async Task<ActionResult<Dto.Session>> GetSession(long sessionId)
	{
        long userId = HttpContext.GetUserId();
		if (!await _sessionService.IsUserSessionMember(userId, sessionId)) return Forbid();
		var session = await _sessionService.GetSession(sessionId);
        return session is null ? NotFound() : Ok(session);
    }
    
	[HttpGet]
	public async Task<ActionResult<IEnumerable<Dto.Session>>> GetRoomSessions(long roomId)
	{
        long userId = HttpContext.GetUserId();
        if (!await _roomService.IsUserRoomMember(userId, roomId)) return Forbid();
        return Ok(await _sessionService.GetRoomSessions(roomId));
	}

}
