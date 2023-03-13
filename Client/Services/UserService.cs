using Concerto.Shared.Extensions;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;

namespace Concerto.Client.Services;

public interface IUserService : IUserClient
{
    public Task<Guid?> UserId();
    public Task<string> GetTokenString();
}

public class UserService : UserClient, IUserService, IDisposable
{
    private readonly AuthenticationStateProvider _authenticationStateProvider;
    private readonly IAccessTokenProvider _tokenProvider;
    private Task<AuthenticationState>? _authenticationStateTask;

    private Guid? _userId;

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

    public async Task<Guid?> UserId()
    {
        if (await IsLoggedIn())
        {
            if (_userId != null) return _userId;
            if (_authenticationStateTask == null) return null;
            var authenticationState = await _authenticationStateTask;
            _userId = authenticationState.User.GetSubjectId();
            return _userId;
        }

        return null;
    }

    public async Task<string> GetTokenString()
    {
        var tokenResult = await _tokenProvider.RequestAccessToken();
		return tokenResult.TryGetToken(out var token) ? token.Value : string.Empty;
    }

    private void AuthenticationStateChanged(Task<AuthenticationState> authenticationState)
    {
        _userId = null;
        _authenticationStateTask = authenticationState;
    }

    private async Task<bool> IsLoggedIn()
    {
        if (_authenticationStateTask == null) return false;

        var authenticationState = await _authenticationStateTask;
        return authenticationState.User.Identity?.IsAuthenticated ?? false;
    }
}


