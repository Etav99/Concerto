using Concerto.Server.Extensions;

namespace Concerto.Server.Data.Models;

public class Conversation : Entity
{
	public bool IsPrivate { get; set; }
	public virtual ICollection<ChatMessage> ChatMessages { get; set; }
	public virtual ICollection<ConversationUser> ConversationUsers { get; set; }
}


public static partial class ViewModelConversions
{
	public static Dto.ConversationListItem ToConversationListItem(this Conversation conversation, long userId)
	{
		return new Dto.ConversationListItem(
			Id: conversation.Id,
			Name: conversation.ConversationUsers.FirstOrDefault(cu => cu.UserId != userId)?.User.FullName ?? string.Empty,
			LastMessage: conversation.ChatMessages.FirstOrDefault()?.ToViewModel()
		);
	}
}