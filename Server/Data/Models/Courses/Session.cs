using Concerto.Server.Extensions;
using System.ComponentModel.DataAnnotations.Schema;

namespace Concerto.Server.Data.Models;

public class Session : Entity
{
    public string Name { get; set; } = null!;
	public DateTime ScheduledDate { get; set; }
	public long CourseId { get; set; }
    public Course Course { get; set; } = null!;
	public long ConversationId { get; set; }
    public Conversation Conversation { get; set; } = null!;
	[DatabaseGenerated(DatabaseGeneratedOption.Identity)]
	public Guid MeetingGuid { get; set; }
}

public static partial class ViewModelConversions
{
    public static Dto.Session ToViewModel(this Session session, bool canManage)
    {
        return new Dto.Session(
            Id: session.Id,
            Name: session.Name,
            CourseId: session.CourseId,
            CourseName: session.Course.Name,
            ScheduledDateTime: session.ScheduledDate,
            CourseRootFolderId: session.Course.RootFolderId,
            ConversationId: session.ConversationId,
            MeetingGuid: session.MeetingGuid,
			CanManage: canManage
		);
    }

    public static Dto.SessionListItem ToSessionListItem(this Session session)
    {
        return new Dto.SessionListItem(
            Id: session.Id,
            Name: session.Name,
            ScheduledDate: session.ScheduledDate
        );
    }

	public static Dto.SessionSettings ToSettingsViewModel(this Session session)
	{
		return new Dto.SessionSettings(
			Id: session.Id,
			Name: session.Name,
			ScheduledDate: session.ScheduledDate
		);
	}

}