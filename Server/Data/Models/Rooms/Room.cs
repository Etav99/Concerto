namespace Concerto.Server.Data.Models;

public class Room : Entity
{
	public string Name { get; set; }

	public virtual ICollection<RoomUser> RoomUsers { get; set; }
	public long ConversationId { get; set; }
	public Conversation Conversation { get; set; }
	public virtual ICollection<Session>? Sessions { get; set; }

}
