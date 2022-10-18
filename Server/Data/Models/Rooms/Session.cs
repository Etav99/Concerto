using System.ComponentModel.DataAnnotations.Schema;

namespace Concerto.Server.Data.Models;

public class Session : Entity
{
	public string Name { get; set; }
	public DateTime ScheduledDate { get; set; }
	public long RoomId { get; set; }
	public Room Room { get; set; }
	public long ConversationId { get; set; }
	public Conversation Conversation { get; set; }
	public virtual ICollection<UploadedFile>? Files { get; set; }
	public virtual ICollection<Catalog> SharedCatalogs { get; set; } = null!;
	[DatabaseGenerated(DatabaseGeneratedOption.Identity)]
	public Guid MeetingGuid { get; set; }
}