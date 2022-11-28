using Concerto.Shared.Extensions;
using Concerto.Shared.Models.Dto;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;

namespace Concerto.Client.Services;

public interface IUserService
{
	public bool IsLoggedIn { get; }
	public long? UserId { get; }
	public void SetAuthenticationState(AuthenticationState authenticationState);
	public Task FetchUserId();
	public Task<IEnumerable<User>> GetUsers();
	public Task<User?> GetUser(long userId);
}

public class UserService : IUserService
{
	private AuthenticationState? _authenticationState;
	private readonly IUserClient _userClient;
	private readonly IAccessTokenProvider _tokenProvider;

	public UserService(IUserClient userClient, IAccessTokenProvider tokenProvider)
	{
		_userClient = userClient;
		_tokenProvider = tokenProvider;
	}

	public bool IsLoggedIn => _authenticationState?.User.Identity?.IsAuthenticated ?? false;
	public long? UserId { get; private set; }

	public void SetAuthenticationState(AuthenticationState authenticationState)
	{
		_authenticationState = authenticationState;
	}

	public async Task FetchUserId()
	{
		if (IsLoggedIn)
		{
			UserId = await _userClient.GetCurrentUserIdAsync();
		}
	}
    
    public async Task<IEnumerable<User>> GetUsers() => await _userClient.GetUsersAsync();

    public async Task<User?> GetUser(long userId) => await _userClient.GetUserAsync(userId);
}