using Concerto.Shared.Models.Dto;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
using Microsoft.AspNetCore.SignalR.Client;

namespace Concerto.Client.Services;

public class ClientNotificationService
{
    private readonly IAccessTokenProvider _accessTokenProvider;
    private readonly NavigationManager _navigationManager;


    public ClientNotificationService(NavigationManager navigationManager, IAccessTokenProvider accessTokenProvider)
    {
        _navigationManager = navigationManager;
        _accessTokenProvider = accessTokenProvider;

        Connection = new HubConnectionBuilder()
            .WithUrl(_navigationManager.ToAbsoluteUri("notifications"), options =>
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

        Connection.On(ReceiveNewPost, (Post post) => { OnNewPostReceived?.Invoke(this, post); });

        Connection.On(ReceivePostUpdate, (Post post) => { OnPostUpdateReceived?.Invoke(this, post); });

        Connection.On(ReceiveNewComment, (Comment comment) => { OnNewCommentReceived?.Invoke(this, comment); });

        Connection.On(ReceiveCommentUpdate, (Comment comment) => { OnCommentUpdateReceived?.Invoke(this, comment); });
    }

    private HubConnection Connection { get; }
    public bool Connected => Connection?.State == HubConnectionState.Connected;
    public bool Disconnected => Connection?.State == HubConnectionState.Disconnected;


    public static string ReceiveNewPost => "ReceiveNewPost";
    public static string ReceivePostUpdate => "ReceivePostUpdate";
    public static string ReceiveNewComment => "ReceiveNewComment";
    public static string ReceiveCommentUpdate => "ReceiveCommentUpdate";


    private EventHandler<Post>? OnNewPostReceived { get; set; }
    private EventHandler<Post>? OnPostUpdateReceived { get; set; }
    private EventHandler<Comment>? OnNewCommentReceived { get; set; }
    private EventHandler<Comment>? OnCommentUpdateReceived { get; set; }


    public async Task SubscribeForum(long courseId)
    {
        await ConnectAsync();
        await Connection.InvokeAsync("SubscribeForum", courseId);
    }

    public async Task UnsubscribeForum(long courseId)
    {
        if (Connected) await Connection.InvokeAsync("UnsubscribeForum", courseId);
    }

    public async Task ConnectAsync()
    {
        if (Disconnected) await Connection.StartAsync();
    }

    public async Task DisconnectAsync()
    {
        await Connection.DisposeAsync();
    }

    public async ValueTask DisposeAsync()
    {
        await Connection.DisposeAsync();
    }
}



