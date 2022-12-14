using Concerto.Server.Middlewares;
using Concerto.Server.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Concerto.Server.Controllers;

[Route("[controller]/[action]")]
[ApiController]
[Authorize]
public class UserController : ControllerBase
{
	private readonly ILogger<UserController> _logger;
	private readonly UserService _userService;
	private long UserId => HttpContext.UserId();


	public UserController(ILogger<UserController> logger, UserService userService)
	{
		_logger = logger;
		_userService = userService;
	}

	[HttpGet]
	public async Task<Dto.User?> GetUser([FromQuery] long userId)
	{
		return await _userService.GetUser(userId);
	}
    
    [HttpGet]
    public long GetCurrentUserId()
    {
        return HttpContext.UserId();
    }

    [HttpPost]
    public async Task<long> AfterLogin()
    {
		return await _userService.GetUserIdAndUpdate(User);
    }

    [HttpGet]
	public async Task<Dto.User?> GetCurrentUser()
	{
        return await _userService.GetUser(UserId);
	}

    [HttpGet]
    public async Task<IEnumerable<Dto.User>> GetUsers()
    {
        return await _userService.GetUsers(UserId);
    }
    
	[HttpGet]
	public async Task<IEnumerable<Dto.User>> Search([FromQuery] string searchString)
	{
		return await _userService.SearchWithoutUser(UserId, searchString);
	}
}
