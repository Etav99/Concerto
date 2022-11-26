using Concerto.Server.Middlewares;
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
	public async Task<IEnumerable<Dto.ConversationListItem>> GetCurrentUserPrivateConversations()
	{
		long userId = HttpContext.UserId();        
		return await _chatService.GetPrivateConversationsAsync(userId);
	}

	[HttpGet]
	public async Task<ActionResult<Dto.Conversation>> GetConversation(long conversationId)
	{
		long userId = HttpContext.UserId();
		if (!User.IsInRole("admin") && !await _chatService.IsUserInCoversationAsync(userId, conversationId)) return Forbid();

		var conversation = await _chatService.GetConversation(conversationId);
		return conversation is not null ? Ok(conversation) : NotFound();
	}

	[HttpGet]
	public async Task<ActionResult<IEnumerable<Dto.ChatMessage>>> GetCurrentUserLastMessages([FromQuery] long conversationId, [FromQuery] long? beforeMessageId)
	{
		// TODO
		const int numberOfMessages = 100;
        long userId = HttpContext.UserId();

        if (!await _chatService.IsUserInCoversationAsync(userId, conversationId)) return Unauthorized();
        return Ok(await _chatService.GetLastMessagesAsync(conversationId, numberOfMessages, beforeMessageId));
	}
}
