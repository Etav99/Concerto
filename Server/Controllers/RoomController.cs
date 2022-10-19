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
        long userId = HttpContext.GetUserId();
		return await _roomService.GetUserRooms(userId);
	}

	[HttpGet]
	public async Task<ActionResult<Dto.Room>> GetRoom(long roomId)
	{
        long userId = HttpContext.GetUserId();
        
		if (!await _roomService.IsUserRoomMember(userId, roomId)) return Forbid();

		var room = await _roomService.GetRoom(roomId);
        if (room == null) return NotFound();
        return Ok(room);
    }

    [Authorize(Roles = "teacher")]
    [HttpPost]
	public async Task<ActionResult> CreateRoomForCurrentUser([FromBody] Dto.CreateRoomRequest room)
	{
        long userId = HttpContext.GetUserId();
       
		if (await _roomService.CreateRoom(room, userId))
		{
			return Ok();
		}
		return BadRequest();
	}



}
