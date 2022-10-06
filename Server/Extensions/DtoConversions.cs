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
            Conversation = room.Conversation.ToDto(),
            Sessions = room.Sessions?.Select(s => s.ToDto()) ?? Enumerable.Empty<Dto.Session>()
        };
    }

    public static Dto.Session ToDto(this Session session)
    {
        return new Dto.Session
        {
            SessionId = session.SessionId,
            Name = session.Name,
            ScheduledDateTime = session.ScheduledDate,
            Conversation = session.Conversation.ToDto()
        };
    }

    public static Conversation ToGroupConversation(this IEnumerable<User> users)
    {
        var conversation = new Conversation();
        conversation.IsPrivate = false;
        conversation.ConversationUsers = users.ToConversationUsers(conversation).ToList();
        return conversation;
    }


    public static IEnumerable<ConversationUser> ToConversationUsers(this IEnumerable<User> users, Conversation conversation)
    {
        return users.Select(u => new ConversationUser
        {
            Conversation = conversation,
            User = u
        });
    }

    public static Dto.UploadedFile ToDto(this UploadedFile file)
    {
        return new Dto.UploadedFile
        {
            Id = file.Id,
            Name = file.DisplayName,
        };
    }
    public static IEnumerable<Dto.UploadedFile> ToDto(this IEnumerable<UploadedFile> files)
    {
        return files.Select(u => u.ToDto());
    }

    public static Dto.FileUploadResult ToDto(this FileUploadResult fileUploadResult)
    {
        return new Dto.FileUploadResult
        {
            DisplayFileName = fileUploadResult.DisplayFileName,
            ErrorCode = fileUploadResult.ErrorCode,
            Uploaded = fileUploadResult.Uploaded
        };
    }

    public static IEnumerable<Dto.FileUploadResult> ToDto(this IEnumerable<FileUploadResult> fileUploadResults)
    {
        return fileUploadResults.Select(u => u.ToDto());
    }



}
