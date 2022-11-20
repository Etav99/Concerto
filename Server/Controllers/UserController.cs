using Concerto.Server.Middlewares;
using Concerto.Server.Services;
using Concerto.Shared.Extensions;
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
    public async Task<bool> AfterLogin()
    {
		return await _userService.AddUserIfNotExists(User);
    }

    [HttpGet]
	public async Task<Dto.User?> GetCurrentUser()
	{
        long userId = HttpContext.UserId();
        return await _userService.GetUser(userId);
	}

	[HttpGet]
	public async Task<IEnumerable<Dto.User>> GetCurrentUserContacts()
	{
        long userId = HttpContext.UserId();
		return await _userService.GetUserContacts(userId);
	}

	[HttpGet]
	public async Task<IEnumerable<Dto.User>> Search([FromQuery] string searchString)
	{
        long userId = HttpContext.UserId();
		return await _userService.SearchWithoutUser(userId, searchString);
	}
}
