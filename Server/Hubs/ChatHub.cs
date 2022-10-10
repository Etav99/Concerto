using Concerto.Server.Extensions;
using Concerto.Server.Services;
using Concerto.Shared.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Concerto.Server.Hubs;

[Authorize]
public class ChatHub : Hub
{

    private readonly ILogger<ChatHub> _logger;
    private readonly ChatService _chatService;


    public ChatHub(ILogger<ChatHub> logger, ChatService chatService)
    {
        _logger = logger;
        _chatService = chatService;
    }

    public override Task OnConnectedAsync()
    {
        string? userId = Context.GetUserIdString();

        if (!string.IsNullOrEmpty(userId))
        {
            _logger.LogDebug($"User with ID {userId} conncted to ChatHub");
            Groups.AddToGroupAsync(Context.ConnectionId, userId);
        }
        else
        {
            throw new Exception("ChatHub connection attempt with empty UserId");
        }
        return base.OnConnectedAsync();
    }

    public async Task SendMessage(long conversationId, string content)
    {
        long? senderId = Context.GetUserId();
        if (!senderId.HasValue)
            return;

        if (!await _chatService.IsUserInCoversationAsync(senderId.Value, conversationId))
            return;

        var message = new Dto.ChatMessage
        {
            SenderId = senderId.Value,
            ConversationId = conversationId,
            SendTimestamp = DateTime.UtcNow,
            Content = content
        };

        List<Task> tasks = new List<Task>();
        await _chatService.SaveMessageAsync(message);
        var receipents = await _chatService.GetReceipentsInConversationAsync(senderId.Value, conversationId);
        foreach (var user in receipents)
        {
            tasks.Add(Clients.Group($"{user.Id}").SendAsync("ReceiveMessage", message));
        }
        await Task.WhenAll(tasks);
    }
}
