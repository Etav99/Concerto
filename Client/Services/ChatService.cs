using Concerto.Shared.Extensions;
using Concerto.Shared.Models.Dto;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
using Microsoft.AspNetCore.SignalR.Client;
using MudBlazor;
using static Concerto.Client.Services.IChatService;

namespace Concerto.Client.Services;

public interface IChatService
{
	public bool Connected { get; }
	public bool Disconnected { get; }
	public Task ConnectToChatAsync();
	public Task DisconnectAsync();
	public Task SendChatMessageAsync(Dto.ChatMessage message);

	public delegate Task AsyncMessageEventHandler(Dto.ChatMessage message);

	public AsyncMessageEventHandler? OnMessageReceivedEventHandler { get; set; }
	public AsyncMessageEventHandler? OnMessageSentEventHandler { get; set; }
	public Task<IEnumerable<ConversationListItem>> GetPrivateConversationsAsync();
	public Task<Conversation> GetConversation(long conversationId);
	public Task<IEnumerable<ChatMessage>> GetCurrentUserLastMessages(long conversationId, long? beforeMessageId);
}

public class ChatService : IChatService
{
	private readonly NavigationManager _navigationManager;
	private readonly IAccessTokenProvider _accessTokenProvider;
	private readonly IChatClient _chatClient;
	private readonly ISnackbar _snackbar;
	
	public AsyncMessageEventHandler? OnMessageReceivedEventHandler { get; set; }
	public AsyncMessageEventHandler? OnMessageSentEventHandler { get; set; }

	private HubConnection ChatHubConnection { get; set; }
	public bool Connected => ChatHubConnection?.State == HubConnectionState.Connected;
	public bool Disconnected => ChatHubConnection?.State == HubConnectionState.Disconnected;

	public ChatService(NavigationManager navigationManager, IChatClient chatClient, IAccessTokenProvider accessTokenProvider, ISnackbar snackbar)
	{
		_navigationManager = navigationManager;
		_chatClient = chatClient;
		_accessTokenProvider = accessTokenProvider;
		_snackbar = snackbar;

		ChatHubConnection = new HubConnectionBuilder()
				.WithUrl(_navigationManager.ToAbsoluteUri("chat"), options =>
				{
					options.AccessTokenProvider = async () =>
					{
						var accessTokenResult = await _accessTokenProvider.RequestAccessToken();
						accessTokenResult.TryGetToken(out var accessToken);
						return accessToken.Value;
					};
				})
				.Build();

		ChatHubConnection.On("ReceiveMessage", async (Dto.ChatMessage message) =>
		{
			if (OnMessageReceivedEventHandler != null) await OnMessageReceivedEventHandler.Invoke(message);
		});

	}

	public async Task ConnectToChatAsync()
	{
		if (Disconnected) await ChatHubConnection.StartAsync();
	}

	public async Task DisconnectAsync()
	{
		await ChatHubConnection.DisposeAsync();
	}

	public async Task<IEnumerable<ConversationListItem>> GetPrivateConversationsAsync()
	{
		return await _chatClient.GetCurrentUserPrivateConversationsAsync();
	}


	public async Task SendChatMessageAsync(Dto.ChatMessage message)
	{
		await ChatHubConnection.SendAsync("SendMessage", message.ConversationId, message.Content);
		if (OnMessageSentEventHandler != null) await OnMessageSentEventHandler.Invoke(message);
	}

	public async ValueTask DisposeAsync()
	{
		if (ChatHubConnection is not null)
		{
			await ChatHubConnection.DisposeAsync();
		}
	}
	
	public async Task<Conversation> GetConversation(long conversationId) => await _chatClient.GetConversationAsync(conversationId);

	public async Task<IEnumerable<ChatMessage>> GetCurrentUserLastMessages(long conversationId, long? beforeMessageId) => await _chatClient.GetCurrentUserLastMessagesAsync(conversationId, beforeMessageId);
}
