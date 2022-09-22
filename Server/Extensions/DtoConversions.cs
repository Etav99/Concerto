using Concerto.Server.Data.Models;

namespace Concerto.Server.Extensions;
public static class DtoConversions
{
    public static Dto.User ToDto(this User user)
    {
        return new Dto.User
        {
            UserId = user.UserId,
            Username = user.Username,
            FirstName = user.FirstName,
            LastName = user.LastName
        };
    }
    
    public static IEnumerable<Dto.User> ToDto(this IEnumerable<User>? users)
    {
        if (users == null)
            return Enumerable.Empty<Dto.User>();
        return users.Select(c => c.ToDto());
    }

    public static Dto.Conversation ToDto(this Conversation conversation)
    {
        return new Dto.Conversation
        {
            ConversationId = conversation.ConversationId,
            IsPrivate = conversation.IsPrivate,
            Users = conversation.ConversationUsers.Select(cu => cu.User).ToDto(),
            LastMessage = conversation.ChatMessages?.FirstOrDefault()?.ToDto()
        };
    }
    public static Dto.Conversation ToDto(this Conversation conversation, long userId)
    {
        return new Dto.Conversation
        {
            ConversationId = conversation.ConversationId,
            IsPrivate = conversation.IsPrivate,
            Users = conversation.ConversationUsers.Select(cu => cu.User).Where(u => u.UserId != userId).ToDto(),
            LastMessage = conversation.ChatMessages.FirstOrDefault()?.ToDto()
        };
    }

    public static Dto.ChatMessage ToDto(this ChatMessage message)
    {
        return new Dto.ChatMessage
        {
            SendTimestamp = message.SendTimestamp,
            SenderId = message.SenderId,
            ConversationId = message.ConversationId,
            Content = message.Content
        };
    }
    
    public static ChatMessage ToModel(this Dto.ChatMessage message, DateTime sendTimestamp)
    {
        return new ChatMessage
        {
            SendTimestamp = sendTimestamp,
            SenderId = message.SenderId,
            ConversationId = message.ConversationId,
            Content = message.Content,
        };
    }


    public static Dto.Room ToDto(this Room room)
    {
        return new Dto.Room
        {
            RoomId = room.RoomId,
            Name = room.Name,
            Users = room.RoomUsers.Select(ru => ru.User.ToDto()),
            Conversation = room.Conversation.ToDto()
        };
    }


}
