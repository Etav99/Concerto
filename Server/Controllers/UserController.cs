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
	public async Task<Dto.User?> GetCurrentUser()
	{
		// Todo add app identity claim in middleware
		long? userId = User.GetUserId();
		if (userId == null) return null;
		return await _userService.GetUser(userId.Value);
	}

	[HttpGet]
	public async Task<IEnumerable<Dto.User>> GetCurrentUserContacts()
	{
		// Todo add app identity claim in middleware
		long? userId = User.GetUserId();
		if (userId == null) return Enumerable.Empty<Dto.User>();
		return await _userService.GetUserContacts(userId.Value);
	}

	[HttpGet]
	public async Task<IEnumerable<Dto.User>> Search([FromQuery] string searchString)
	{
		long? userId = User.GetUserId();
		if (userId == null) return Enumerable.Empty<Dto.User>();
		return await _userService.SearchWithoutUser(userId.Value, searchString);
	}
}
