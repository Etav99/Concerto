using Concerto.Server.Middlewares;
using Concerto.Server.Services;
using Concerto.Shared.Models.Dto;
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

	private long UserId => HttpContext.UserId();

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

	[Authorize(Roles = "admin")]
	[HttpGet]
	public async Task<IEnumerable<Dto.UserIdentity>> GetUnverifiedUserIdentities()
	{
		return await _userService.GetUnverifiedUserIdentities();
	}

	[Authorize(Roles = "admin")]
	[HttpGet]
	public async Task<IEnumerable<Dto.UserIdentity>> GetUserIdentities()
	{
		return await _userService.GetUserIdentities(UserId);
	}
	
	[Authorize(Roles = "admin")]
	[HttpPost]
	public async Task VerifiyUser(Guid subjectId)
	{
		await _userService.VerifyUser(subjectId);
	}

	[Authorize(Roles = "admin")]
	[HttpPost]
	public async Task SetUserRole(Guid subjectId, Role role)
	{
		await _userService.SetUserRole(subjectId, role);
	}

	[Authorize(Roles = "admin")]
	[HttpDelete]
	public async Task DeleteUser(Guid subjectId)
	{
		await _userService.DeleteUser(subjectId);
	}
}

