using Concerto.Server.Data.DatabaseContext;
using Concerto.Server.Data.Models;
using Concerto.Server.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Concerto.Server.Services;

public class ChatService
{
	private readonly ILogger<ChatService> _logger;
	private readonly AppDataContext _context;

	public ChatService(ILogger<ChatService> logger, AppDataContext context)
	{
		_context = context;
		_logger = logger;
	}

	public async Task SaveMessageAsync(Dto.ChatMessage message)
	{
		ChatMessage messageModel = message.ToModel(DateTime.UtcNow);
		await _context.ChatMessages.AddAsync(messageModel);
		await _context.SaveChangesAsync();
	}

	internal async Task<Dto.Conversation?> GetConversation(long conversationId)
	{
		var conversation = await _context.Conversations.FindAsync(conversationId);
		if (conversation == null) return null;

		await _context.Entry(conversation)
			.Collection(c => c.ConversationUsers)
			.Query()
			.Include(cu => cu.User)
			.LoadAsync();

		conversation.ChatMessages = await _context.ChatMessages
			.Where(cm => cm.ConversationId == conversation.Id)
			.OrderByDescending(cm => cm.Id)
			.Take(20)
			.ToListAsync();

		return conversation.ToViewModel();
	}

	public async Task<IEnumerable<Dto.ConversationListItem>> GetPrivateConversationsAsync(long userId)
	{
		return await _context.ConversationUsers
			.Where(cu => cu.UserId == userId)
			.Select(cu => cu.Conversation)
			.Where(c => c.IsPrivate)
			.Include(c => c.ConversationUsers).ThenInclude(cu => cu.User)
			.Include(c => c.ChatMessages.OrderByDescending(cm => cm.Id).Take(1))
			.Select(c => c.ToConversationListItem(userId))
			.ToListAsync();
	}

	public async Task<IEnumerable<Dto.ChatMessage>> GetLastMessagesAsync(long conversationId, int numberOfMessages, long? beforeMessageId = null)
	{
		var messagesQuery = _context.ChatMessages
			.Where(cm => cm.ConversationId == conversationId);

		if (beforeMessageId != null)
			messagesQuery = messagesQuery.Where(m => m.Id < beforeMessageId);

			return await messagesQuery
			.OrderByDescending(cm => cm.Id)
			.Take(numberOfMessages)
			.Select(cm => cm.ToViewModel())
			.ToListAsync();
	}

	public async Task<bool> IsUserInCoversationAsync(long userId, long conversationId)
	{
		var conversation = await _context.Conversations
			.FindAsync(conversationId);

		if (conversation == null)
			return false;

		await _context.Entry(conversation).Collection(c => c.ConversationUsers).LoadAsync();

		return await _context.Entry(conversation)
			.Collection(c => c.ConversationUsers)
			.Query()
			.AnyAsync(cu => cu.UserId == userId);
	}

	public async Task<IEnumerable<User>> GetReceipentsInConversationAsync(long senderId, long conversationId)
	{
		var conversation = await _context.Conversations
			.FindAsync(conversationId);

		if (conversation == null)
			return Enumerable.Empty<User>();

		return await _context.Entry(conversation)
			.Collection(c => c.ConversationUsers)
			.Query()
			.Where(cu => cu.UserId != senderId)
			.Include(cu => cu.User)
			.Select(cu => cu.User)
			.ToListAsync();
	}
}

