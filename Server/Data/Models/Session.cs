using System.ComponentModel.DataAnnotations;

namespace Concerto.Server.Data.Models;

public class Session
{
    [Key]
    public long SessionId { get; set; }
    public string Name { get; set; }
    public DateTime ScheduledDate { get; set; }
    public long RoomId { get; set; }
    public Room Room { get; set; }
    public long ConversationId { get; set; }
    public Conversation Conversation { get; set; }
    public virtual ICollection<UploadedFile>? Files { get; set; }
}