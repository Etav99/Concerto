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
	private readonly UserService _userService;


	public ChatHub(ILogger<ChatHub> logger, ChatService chatService, UserService userService)
	{
		_logger = logger;
		_chatService = chatService;
		_userService = userService;
	}

	public override async Task OnConnectedAsync()
	{
		Guid? userSubId = Context.User?.GetSubjectId();

		if (userSubId is not null)
		{
            var userId = (await _userService.GetUserId(userSubId.Value)).ToString();
            _logger.LogDebug($"User with ID {userId} conncted to ChatHub");
			await Groups.AddToGroupAsync(Context.ConnectionId, userId!);
		}
		else
		{
			throw new Exception("ChatHub connection attempt with empty UserId");
		}
		
		await base.OnConnectedAsync();
	}

	public async Task SendMessage(long conversationId, string content)
	{
        Guid? senderSubId = Context.User?.GetSubjectId();
        if (senderSubId is null)
            return;

        long? senderId = await _userService.GetUserId(senderSubId.Value);
        
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
