using System.ComponentModel.DataAnnotations;

namespace Concerto.Server.Data.Models;
public class ChatMessage : Entity
{
	[Required]
	public long SenderId { get; set; }
	public User Sender { get; set; }

	[Required]
	public long ConversationId { get; set; }
	public Conversation Conversation { get; set; }

	[Required]
	public DateTime SendTimestamp { get; set; }

	[Required]
	public string Content { get; set; }
}
