using Concerto.Server.Services;
using Concerto.Shared.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Concerto.Server.Controllers;

[Route("[controller]/[action]")]
[ApiController]
[Authorize]
public class ChatController : ControllerBase
{
    private readonly ILogger<ChatController> _logger;
    private readonly ChatService _chatService;

    public ChatController(ILogger<ChatController> logger, ChatService chatService)
    {
        _logger = logger;
        _chatService = chatService;
    }

    [HttpGet]
    public async Task<IEnumerable<Dto.Conversation>> GetCurrentUserPrivateConversations()
    {
        long? userId = User.GetUserId();
        if (userId == null) return Enumerable.Empty<Dto.Conversation>();
        return await _chatService.GetPrivateConversationsAsync(userId.Value);

    }

    [HttpGet]
    public async Task<IEnumerable<Dto.ChatMessage>> GetCurrentUserLastMessages([FromQuery] long conversationId)
    {

        const int numberOfMessages = 100;

        long? userId = User.GetUserId();
        // TODO
        // Check if user is in conversation

        if (userId == null) return Enumerable.Empty<Dto.ChatMessage>();
        return await _chatService.GetLastMessagesAsync(conversationId, numberOfMessages);

    }

    [HttpGet]
    public async Task<IEnumerable<Dto.ChatMessage>> GetCurrentUserLastMessagesBefore([FromQuery] long conversationId, [FromQuery] DateTime startingMessageTimestamp)
    {
        // TODO move this to config
        const int numberOfMessages = 100;
        long? userId = User.GetUserId();
        // TODO
        // Check if user is in conversation

        if (userId == null) return Enumerable.Empty<Dto.ChatMessage>();
        return await _chatService.GetLastMessagesBeforeAsync(conversationId, startingMessageTimestamp, numberOfMessages);
    }

}
