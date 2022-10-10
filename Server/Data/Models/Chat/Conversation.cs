using System.ComponentModel.DataAnnotations;

namespace Concerto.Server.Data.Models;

public class Conversation
{
    [Key]
    public long ConversationId { get; set; }
    public bool IsPrivate { get; set; }
    public virtual ICollection<ChatMessage> ChatMessages { get; set; }
    public virtual ICollection<ConversationUser> ConversationUsers { get; set; }
}
