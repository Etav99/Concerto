using Concerto.Server.Extensions;

namespace Concerto.Server.Data.Models;

public class Room : Entity
{
	public string Name { get; set; }
	public string Description { get; set; } = string.Empty;
	public long OwnerId { get; set; }
	public virtual ICollection<RoomUser> RoomUsers { get; set; }
	public long ConversationId { get; set; }
	public Conversation Conversation { get; set; }
	public virtual ICollection<Session>? Sessions { get; set; }
}

public static partial class ViewModelConversions
{
    public static Dto.Room ToDto(this Room room)
    {
        return new Dto.Room
        {
            Id = room.Id,
            OwnerId = room.OwnerId,
            Name = room.Name,
            Users = room.RoomUsers.Select(ru => ru.User.ToDto()),
            Conversation = room.Conversation.ToDto(),
            Sessions = room.Sessions?.Select(s => s.ToDto()) ?? Enumerable.Empty<Dto.Session>(),
        };
    }
}