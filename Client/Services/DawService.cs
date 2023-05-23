using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
using Microsoft.AspNetCore.SignalR.Client;

namespace Concerto.Client.Services;

public class DawService : DawClient
{
    private IAccessTokenProvider _accessTokenProvider;
    private NavigationManager _navigationManager;

    public DawService(HttpClient httpClient, IAccessTokenProvider accessTokenProvider, NavigationManager navigationManager) : base(httpClient)
    {
        _accessTokenProvider = accessTokenProvider;
        _navigationManager = navigationManager;
    }

    public HubConnection CreateHubConnection()
    {
        return new HubConnectionBuilder()
            .WithUrl(_navigationManager.ToAbsoluteUri("daw"), options =>
                {
                    options.AccessTokenProvider = async () =>
                    {
                        var accessTokenResult = await _accessTokenProvider.RequestAccessToken();
                        accessTokenResult.TryGetToken(out var accessToken);
                        return accessToken.Value;
                    };
                }
            )
            .Build();
    }
}