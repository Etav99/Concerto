using Concerto.Server.Data.Models;

namespace Concerto.Server.Extensions;
public static class ViewModelConversions
{
	public static Dto.User ToViewModel(this User user)
	{
		return new Dto.User
		{
			Id = user.Id,
			Username = user.Username,
			FirstName = user.FirstName,
			LastName = user.LastName
		};
	}

	public static IEnumerable<Dto.User> ToViewModel(this IEnumerable<User>? users)
	{
		if (users == null)
			return Enumerable.Empty<Dto.User>();
		return users.Select(c => c.ToViewModel());
	}

	public static Dto.Conversation ToViewModel(this Conversation conversation, long? withoutUserId = null)
	{
		return new Dto.Conversation
		{
			Id = conversation.Id,
			IsPrivate = conversation.IsPrivate,
			Users = withoutUserId.HasValue ? conversation.ConversationUsers.Select(cu => cu.User).Where(u => u.Id != withoutUserId).ToViewModel()
										   : conversation.ConversationUsers.Select(cu => cu.User).ToViewModel(),
			Messages = conversation.ChatMessages.Select(cm => cm.ToViewModel()).ToList(),
		};
	}

	public static Dto.ChatMessage ToViewModel(this ChatMessage message)
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

	public static Dto.UploadedFile ToViewModel(this UploadedFile file)
	{
		return new Dto.UploadedFile(
			Id: file.Id,
			Name: file.DisplayName
		);
	}
	public static IEnumerable<Dto.UploadedFile> ToViewModel(this IEnumerable<UploadedFile> files)
	{
		return files.Select(u => u.ToViewModel());
	}

	public static Dto.FileUploadResult ToViewModel(this FileUploadResult fileUploadResult)
	{
		return new Dto.FileUploadResult
		{
			DisplayFileName = fileUploadResult.DisplayFileName,
			ErrorCode = fileUploadResult.ErrorCode,
			Uploaded = fileUploadResult.Uploaded
		};
	}

	public static IEnumerable<Dto.FileUploadResult> ToViewModel(this IEnumerable<FileUploadResult> fileUploadResults)
	{
		return fileUploadResults.Select(u => u.ToViewModel());
	}


}
