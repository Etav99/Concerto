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
	private readonly RoomService _roomService;
	private readonly SessionService _sessionService;


	public SessionController(ILogger<SessionController> logger, RoomService roomService, SessionService sessionService)
	{
		_logger = logger;
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
	public async Task<ActionResult<IEnumerable<Dto.Session?>>> GetRoomSessions(long roomId)
	{
		long? userId = User.GetUserId();
		if (userId == null) return Unauthorized();

		if (!await _roomService.IsUserRoomMember(userId.Value, roomId)) return Unauthorized();

		return Ok(await _sessionService.GetRoomSessions(roomId));
	}

}
