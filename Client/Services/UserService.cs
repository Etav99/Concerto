using Concerto.Shared.Extensions;
using Microsoft.AspNetCore.Components.Authorization;
using System;

namespace Concerto.Client.Services;

public interface IUserService
{
    public bool IsLoggedIn { get; }
    public long? UserId { get; }
    public void SetAuthenticationState(AuthenticationState authenticationState);
}

public class UserService : IUserService
{
    private AuthenticationState? _authenticationState;
    public bool IsLoggedIn => _authenticationState?.User.Identity?.IsAuthenticated ?? false;
    public long? UserId => _authenticationState?.User.GetUserId();

    public void SetAuthenticationState(AuthenticationState authenticationState)
    {
        _authenticationState = authenticationState;
    }
}