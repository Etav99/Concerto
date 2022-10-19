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
	public async Task<IEnumerable<Dto.Conversation>> GetCurrentUserPrivateConversations()
	{
		long userId = HttpContext.GetUserId();        
		return await _chatService.GetPrivateConversationsAsync(userId);

	}

	[HttpGet]
	public async Task<ActionResult<IEnumerable<Dto.ChatMessage>>> GetCurrentUserLastMessages([FromQuery] long conversationId)
	{
		const int numberOfMessages = 100;
        long userId = HttpContext.GetUserId();

        if (!await _chatService.IsUserInCoversationAsync(userId, conversationId)) return Unauthorized();
        // if (userId == null) return Enumerable.Empty<Dto.ChatMessage>();
        return Ok(await _chatService.GetLastMessagesAsync(conversationId, numberOfMessages));
	}

	[HttpGet]
	public async Task<ActionResult<IEnumerable<Dto.ChatMessage>>> GetCurrentUserLastMessagesBefore([FromQuery] long conversationId, [FromQuery] DateTime startingMessageTimestamp)
	{
		// TODO move this to config
		const int numberOfMessages = 100;
        long userId = HttpContext.GetUserId();
        if (!await _chatService.IsUserInCoversationAsync(userId, conversationId)) return Unauthorized();
		return Ok(await _chatService.GetLastMessagesBeforeAsync(conversationId, startingMessageTimestamp, numberOfMessages));
	}

}
