using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR.Client;
using Concerto.Shared.Models.Dto;

namespace Concerto.Shared.Client.Services;

public class ClientNotificationService
{
	private readonly NavigationManager _navigationManager;
	private readonly IAccessTokenProvider _accessTokenProvider;
	private HubConnection Connection { get; set; }
	public bool Connected => Connection?.State == HubConnectionState.Connected;
	public bool Disconnected => Connection?.State == HubConnectionState.Disconnected;


	public static string ReceiveNewPost => "ReceiveNewPost";
	public static string ReceivePostUpdate => "ReceivePostUpdate";
	public static string ReceiveNewComment => "ReceiveNewComment";
	public static string ReceiveCommentUpdate => "ReceiveCommentUpdate";
	

	EventHandler<Post>? OnNewPostReceived { get; set; }
	EventHandler<Post>? OnPostUpdateReceived { get; set; }
	EventHandler<Comment>? OnNewCommentReceived { get; set; }
	EventHandler<Comment>? OnCommentUpdateReceived { get; set; }


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
				})
				.Build();

		Connection.On(ReceiveNewPost, (Post post) =>
		{
			OnNewPostReceived?.Invoke(this, post);
		});

		Connection.On(ReceivePostUpdate, (Post post) =>
		{
			OnPostUpdateReceived?.Invoke(this, post);
		});

		Connection.On(ReceiveNewComment, (Comment comment) =>
		{
			OnNewCommentReceived?.Invoke(this, comment);
		});

		Connection.On(ReceiveCommentUpdate, (Comment comment) =>
		{
			OnCommentUpdateReceived?.Invoke(this, comment);
		});

	}


	public async Task SubscribeForum(long courseId)
	{
		await ConnectAsync();
		await Connection.InvokeAsync("SubscribeForum", courseId);
	}

	public async Task UnsubscribeForum(long courseId)
	{
		if(Connected) await Connection.InvokeAsync("UnsubscribeForum", courseId);
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
		if (Connection is not null)
		{
			await Connection.DisposeAsync();
		}
	}
}
