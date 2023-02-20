using Concerto.Server.Middlewares;
using Concerto.Server.Services;
using Concerto.Shared.Extensions;
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
	private Guid UserId => HttpContext.UserId();

	public UserController(ILogger<UserController> logger, UserService userService)
	{
		_logger = logger;
		_userService = userService;
	}


	[HttpGet]
	public async Task<Dto.User?> GetUser([FromQuery] Guid userId)
	{
		return await _userService.GetUser(userId);
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


[Route("[controller]/[action]")]
[ApiController]
[Authorize(Policy = AuthorizationPolicies.IsAuthenticated.Name)]
public class AccountController : ControllerBase
{
	private readonly ILogger<UserController> _logger;
	private readonly UserService _userService;
	private Guid UserId => HttpContext.UserId();

	public AccountController(ILogger<UserController> logger, UserService userService)
	{
		_logger = logger;
		_userService = userService;
	}

	[HttpPost]
	public async Task AfterLogin()
	{
		await _userService.UpdateUser(User);
	}

	[Authorize(Policy = AuthorizationPolicies.IsAdmin.Name)]
	[HttpGet]
	public async Task<IEnumerable<Dto.UserIdentity>> GetUnverifiedUserIdentities()
	{
		return await _userService.GetUnverifiedUserIdentities();
	}

	[Authorize(Policy = AuthorizationPolicies.IsAdmin.Name)]
	[HttpGet]
	public async Task<IEnumerable<Dto.UserIdentity>> GetUserIdentities()
	{
		return await _userService.GetUserIdentities(UserId);
	}

	[Authorize(Policy = AuthorizationPolicies.IsAdmin.Name)]
	[HttpPost]
	public async Task VerifiyUser(Guid subjectId)
	{
		await _userService.VerifyUser(subjectId);
	}

	[Authorize(Policy = AuthorizationPolicies.IsAdmin.Name)]
	[HttpPost]
	public async Task SetUserRole(Guid subjectId, Role role)
	{
		await _userService.SetUserRole(subjectId, role);
	}

	[Authorize(Policy = AuthorizationPolicies.IsAdmin.Name)]
	[HttpDelete]
	public async Task DeleteUser(Guid subjectId)
	{
		await _userService.DeleteUser(subjectId);
	}

}
