using Concerto.Shared.Extensions;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
using Microsoft.AspNetCore.SignalR.Client;
using MudBlazor;
using System.Net.Http.Json;

namespace Concerto.Client.Services;

public interface IChatManager
{
    public bool IsConnected { get; }
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

public class CachedChatManager : IChatManager
{
    private readonly AuthenticationStateProvider _authenticationStateProvider;
    private readonly NavigationManager _navigationManager;
    private readonly IAccessTokenProvider _accessTokenProvider;
    private readonly HttpClient _http;
    private readonly IContactManager _contactsManager;
    private readonly ISnackbar _snackbar;

    public IChatManager.OnMessageEventCallback OnMessageReceivedCallback { get; set; } = delegate { };
    public IChatManager.OnMessageEventCallback OnMessageSentCallback { get; set; } = delegate { };

    private HubConnection ChatHubConnection { get; set; }
    public bool IsConnected
    {
        get
        {
            return ChatHubConnection?.State == HubConnectionState.Connected;
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

    public CachedChatManager(AuthenticationStateProvider authenticationStateProvider, NavigationManager navigationManager, HttpClient http, IContactManager contactsManager, IAccessTokenProvider accessTokenProvider, ISnackbar snackbar)
    {
        _authenticationStateProvider = authenticationStateProvider;
        _navigationManager = navigationManager;
        _http = http;
        _contactsManager = contactsManager;
        _accessTokenProvider = accessTokenProvider;
        _snackbar = snackbar;

        ChatHubConnection = new HubConnectionBuilder()
                .WithUrl(_navigationManager.ToAbsoluteUri("/chat"), options =>
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
            if (!(_navigationManager.Uri.Contains("chat") || _navigationManager.Uri.Contains("rooms/")))
            {
                _snackbar.Add($"{name}: {message.Content.Truncate(20)}", Severity.Info);
            }
            OnMessageReceivedCallback(message);
        });

    }

    public async Task ConnectToChatAsync()
    {
        if (IsConnected)
            return;
        AuthenticationState authenticationState = await _authenticationStateProvider.GetAuthenticationStateAsync();
        if (authenticationState.User.Identity?.IsAuthenticated == true)
        {
            await ChatHubConnection.StartAsync();
        }
    }

    public async Task LoadConversationsAsync()
    {
        await semaphore.WaitAsync();
        if (conversationCacheInvalidated)
        {
            var conversationsResponse = await _http.GetFromJsonAsync<Dto.Conversation[]>($"Chat/GetCurrentUserPrivateConversations");
            var conversations = conversationsResponse?.ToDictionary(c => c.ConversationId, c => c) ?? new Dictionary<long, Dto.Conversation>();
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
            var messagesResponse = await _http.GetFromJsonAsync<Dto.ChatMessage[]>($"Chat/GetCurrentUserLastMessages?conversationId={conversationId}");
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
        messagesCache[message.ConversationId].Insert(0, message);
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
