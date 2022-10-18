namespace Concerto.Server.Data.Models;

public class ConversationUser
{
	public long ConversationId { get; set; }
	public Conversation Conversation { get; set; }

	public long UserId { get; set; }
	public User User { get; set; }
}
