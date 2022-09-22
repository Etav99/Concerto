using Concerto.Shared.Extensions;
using Concerto.Server.Services;
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
}
