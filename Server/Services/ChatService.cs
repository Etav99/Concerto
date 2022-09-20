﻿using Concerto.Server.Data.DatabaseContext;
using Concerto.Server.Data.Models;
using Concerto.Server.Extensions;
using Microsoft.EntityFrameworkCore;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.Serialization;

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

    public async Task<IEnumerable<Dto.Conversation>> GetPrivateConversationsAsync(long userId)
    {
        return await _context.Conversations
            .Where(c => c.IsPrivate)
            .Include(c => c.ConversationUsers)
            .ThenInclude(cu => cu.User)
            .Where(c => c.ConversationUsers.Any(cu => cu.UserId == userId))
            .Include(c => c.ChatMessages.OrderByDescending(x => x.SendTimestamp).Take(1))
            .Select(c => c.ToDto(userId)).ToListAsync();
    }

    public async Task<IEnumerable<Dto.ChatMessage>> GetLastMessagesAsync(long conversationId, int numberOfMessages)
    {
        IEnumerable<Dto.ChatMessage>? messages = await _context.ChatMessages
            .Where(cm => (cm.ConversationId == conversationId))
            .OrderByDescending(cm => cm.SendTimestamp)
            .Take(numberOfMessages)
            .Select(cm => cm.ToDto())
            .ToListAsync();
        return messages ?? Enumerable.Empty<Dto.ChatMessage>();
    }

    public async Task<IEnumerable<Dto.ChatMessage>> GetLastMessagesBeforeAsync(long conversationId, DateTime startingMessageTimestamp, int numberOfMessages)
    {
        IEnumerable<Dto.ChatMessage>? messages = await _context.ChatMessages
            .Where(cm => (cm.SendTimestamp <= startingMessageTimestamp) && (cm.ConversationId == conversationId))
            .OrderByDescending(cm => cm.SendTimestamp)
            .Take(numberOfMessages)
            .Select(cm => cm.ToDto())
            .ToListAsync();
        return messages ?? Enumerable.Empty<Dto.ChatMessage>();
    }

    public async Task<bool> IsUserInCoversationAsync(long userId, long conversationId)
    {
        return await _context.Conversations
                 .Where(c => c.ConversationId == conversationId)
                 .Include(c => c.ConversationUsers)
                 .AnyAsync(c => c.ConversationUsers.Any(cu => cu.UserId == userId));
    }

    public async Task<IEnumerable<User>> GetReceipentsInConversationAsync(long senderId, long conversationId)
    {
        var a = await _context.Conversations
            .Where(c => c.ConversationId == conversationId)
            .Include(c => c.ConversationUsers)
            .ThenInclude(cu => cu.User)
            .SingleAsync();

        return a.ConversationUsers.Where(cu => cu.UserId != senderId).Select(cu => cu.User);
    }
}
    
