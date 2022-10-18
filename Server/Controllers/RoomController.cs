using Concerto.Server.Extensions;
using Concerto.Server.Services;
using Concerto.Shared.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Concerto.Server.Controllers;

[Route("[controller]/[action]")]
[ApiController]
[Authorize]
public class RoomController : ControllerBase
{
	private readonly ILogger<RoomController> _logger;
	private readonly RoomService _roomService;


	public RoomController(ILogger<RoomController> logger, RoomService roomService)
	{
		_logger = logger;
		_roomService = roomService;
	}

	[HttpGet]
	public async Task<IEnumerable<Dto.Room>> GetCurrentUserRooms()
	{
		long? userId = User.GetUserId();
		if (userId == null) return Enumerable.Empty<Dto.Room>();
		return await _roomService.GetUserRooms(userId.Value);
	}

	[HttpGet]
	public async Task<Dto.Room?> GetRoom(long roomId)
	{
		long? userId = User.GetUserId();
		if (userId == null) return null;
		if (!await _roomService.IsUserRoomMember(userId.Value, roomId)) return null;

		var room = await _roomService.GetRoom(roomId);
		return room;
	}

	[HttpPost]
	public async Task<ActionResult> CreateRoomForCurrentUser([FromBody] Dto.CreateRoomRequest room)
	{
		long? userId = User.GetUserId();
		if (userId == null) return Unauthorized();

		if (await _roomService.CreateRoom(room, userId.Value))
		{
			return Ok();
		}
		return BadRequest();
	}



}
