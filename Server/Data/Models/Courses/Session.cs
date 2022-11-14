using Concerto.Server.Extensions;
using System.ComponentModel.DataAnnotations.Schema;

namespace Concerto.Server.Data.Models;

public class Session : Entity
{
	public string Name { get; set; }
	public DateTime ScheduledDate { get; set; }
	public long CourseId { get; set; }
	public Course Course { get; set; }
	public long ConversationId { get; set; }
	public Conversation Conversation { get; set; }
	[DatabaseGenerated(DatabaseGeneratedOption.Identity)]
	public Guid MeetingGuid { get; set; }
}

public static partial class ViewModelConversions
{
    public static Dto.Session ToDto(this Session session)
    {
        return new Dto.Session
        {
            Id = session.Id,
            Name = session.Name,
            CourseId = session.CourseId,
            CourseOwnerId = session.Course.OwnerId,
            ScheduledDateTime = session.ScheduledDate,
            Conversation = session.Conversation?.ToDto(),
            MeetingGuid = session.MeetingGuid,
        };
    }
}