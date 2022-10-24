using Concerto.Shared.Extensions;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
using Microsoft.AspNetCore.SignalR.Client;
using MudBlazor;

namespace Concerto.Client.Services;

public interface IChatService
{
	public bool Connected { get; }
	public bool Disconnected { get; }
	public Task ConnectToChatAsync();
	public Task SendChatMessageAsync(Dto.ChatMessage message);

	public delegate void OnMessageEventCallback(Dto.ChatMessage message);
	public OnMessageEventCallback OnMessageReceivedCallback { get; set; }
	public OnMessageEventCallback OnMessageSentCallback { get; set; }

	public Dictionary<long, Dto.Conversation> Conversations { get; }
	public Dictionary<long, List<Dto.ChatMessage>> Messages { get; }

	public Task LoadChatMessagesAsync(long contactId);
	public Task LoadConversationsAsync();
	public void InvalidateCache();
}

public class ChatService : IChatService
{
	private readonly NavigationManager _navigationManager;
	private readonly IAccessTokenProvider _accessTokenProvider;
	private readonly IChatClient _chatClient;
	private readonly IContactService _contactsManager;
	private readonly ISnackbar _snackbar;

	public IChatService.OnMessageEventCallback OnMessageReceivedCallback { get; set; } = delegate { };
	public IChatService.OnMessageEventCallback OnMessageSentCallback { get; set; } = delegate { };

	private HubConnection ChatHubConnection { get; set; }
	public bool Connected
	{
		get
		{
			return ChatHubConnection?.State == HubConnectionState.Connected;
		}
	}
	public bool Disconnected
	{
		get
		{
			return ChatHubConnection?.State == HubConnectionState.Disconnected;
		}
	}

	private SemaphoreSlim semaphore = new SemaphoreSlim(1, 1);

	private bool conversationCacheInvalidated = true;
	private Dictionary<long, Dto.Conversation> conversationsCache = new Dictionary<long, Dto.Conversation>();
	public Dictionary<long, Dto.Conversation> Conversations
	{
		get
		{
			return conversationsCache;
		}
	}

	private Dictionary<long, List<Dto.ChatMessage>> messagesCache = new Dictionary<long, List<Dto.ChatMessage>>();
	public Dictionary<long, List<Dto.ChatMessage>> Messages
	{
		get
		{
			return messagesCache;
		}
	}

	public ChatService(NavigationManager navigationManager, IChatClient chatClient, IContactService contactsManager, IAccessTokenProvider accessTokenProvider, ISnackbar snackbar)
	{
		_navigationManager = navigationManager;
		_chatClient = chatClient;
		_contactsManager = contactsManager;
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
			if (messagesCache.ContainsKey(message.ConversationId))
			{
				messagesCache[message.ConversationId].Insert(0, message);
			}
			string name = await _contactsManager.GetContactNameAsync(message.SenderId);
			if (!(_navigationManager.Uri.Contains("chat")))
			{
				_snackbar.Add($"{name}: {message.Content.Truncate(20)}", Severity.Info);
			}
			OnMessageReceivedCallback(message);
		});

	}

	public async Task ConnectToChatAsync()
	{
		if (Disconnected) await ChatHubConnection.StartAsync();
	}

	public async Task LoadConversationsAsync()
	{
		await semaphore.WaitAsync();
		if (conversationCacheInvalidated)
		{
			var conversationsResponse = await _chatClient.GetCurrentUserPrivateConversationsAsync();
			var conversations = conversationsResponse?.ToDictionary(c => c.Id, c => c) ?? new Dictionary<long, Dto.Conversation>();
			conversationsCache = conversations;
			conversationCacheInvalidated = false;
		}
		semaphore.Release();
	}
	public async Task LoadChatMessagesAsync(long conversationId)
	{
		await semaphore.WaitAsync();
		if (!messagesCache.ContainsKey(conversationId))
		{
			var messagesResponse = await _chatClient.GetCurrentUserLastMessagesAsync(conversationId);
			var messages = messagesResponse?.ToList() ?? new List<Dto.ChatMessage>();
			messages.Sort((x, y) => DateTime.Compare(y.SendTimestamp, x.SendTimestamp));
			messagesCache.Add(conversationId, messages);
		}
		semaphore.Release();
	}

	public async Task SendChatMessageAsync(Dto.ChatMessage message)
	{
		if (conversationsCache.ContainsKey(message.ConversationId))
		{
			conversationsCache[message.ConversationId].LastMessage = message;
		}
		if (messagesCache.ContainsKey(message.ConversationId))
		{
			messagesCache[message.ConversationId].Insert(0, message);
		}
		await ChatHubConnection.SendAsync("SendMessage", message.ConversationId, message.Content);
		OnMessageSentCallback(message);
	}

	public void InvalidateCache()
	{
		conversationCacheInvalidated = true;
		messagesCache.Clear();
	}

	public async ValueTask DisposeAsync()
	{
		if (ChatHubConnection is not null)
		{
			await ChatHubConnection.DisposeAsync();
		}
	}
}
