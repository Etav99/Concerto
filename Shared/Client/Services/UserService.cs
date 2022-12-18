using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;

namespace Concerto.Shared.Client.Services;

public interface IUserService : IUserClient
{
	public Task<long?> UserId();
}

public class UserService : UserClient, IUserService, IDisposable
{
	private readonly AuthenticationStateProvider _authenticationStateProvider;
	private readonly IAccessTokenProvider _tokenProvider;
	private Task<AuthenticationState>? _authenticationStateTask;

	private long? _userId;

	public UserService(HttpClient httpClient, IAccessTokenProvider tokenProvider, AuthenticationStateProvider authenticationStateProvider)
		: base(httpClient)
	{
		_tokenProvider = tokenProvider;
		_authenticationStateProvider = authenticationStateProvider;
		_authenticationStateProvider.AuthenticationStateChanged += AuthenticationStateChanged;
	}

	public void Dispose()
	{
		_authenticationStateProvider.AuthenticationStateChanged -= AuthenticationStateChanged;
	}

	public async Task<long?> UserId()
	{
		if (await IsLoggedIn())
		{
			if (_userId != null) return _userId;
			_userId = await GetCurrentUserIdAsync();
			return _userId;
		}

		return null;
	}

	private void AuthenticationStateChanged(Task<AuthenticationState> authenticationState)
	{
		_userId = null;
		_authenticationStateTask = authenticationState;
	}

	public async Task<bool> IsLoggedIn()
	{
		if (_authenticationStateTask != null)
		{
			var authenticationState = await _authenticationStateTask;
			return authenticationState.User.Identity?.IsAuthenticated ?? false;
		}

		return false;
	}
}


