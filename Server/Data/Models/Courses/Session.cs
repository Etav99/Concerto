using System.ComponentModel.DataAnnotations.Schema;
using Concerto.Shared.Models.Dto;

namespace Concerto.Server.Data.Models;

public class Session : Entity
{
	public string Name { get; set; } = null!;
	public DateTime ScheduledDate { get; set; }
	public long CourseId { get; set; }
	public Course Course { get; set; } = null!;

	[DatabaseGenerated(DatabaseGeneratedOption.Identity)]
	public Guid MeetingGuid { get; set; }
}

public static partial class ViewModelConversions
{
	public static Dto.Session ToViewModel(this Session session, bool canManage)
	{
		return new Dto.Session(session.Id,
			session.Name,
			CourseId: session.CourseId,
			CourseName: session.Course.Name,
			ScheduledDateTime: session.ScheduledDate,
			CourseRootFolderId: session.Course.RootFolderId,
			MeetingGuid: session.MeetingGuid,
			CanManage: canManage
		);
	}

	public static SessionListItem ToSessionListItem(this Session session)
	{
		return new SessionListItem(session.Id,
			session.Name,
			session.ScheduledDate
		);
	}

	public static SessionSettings ToSettingsViewModel(this Session session)
	{
		return new SessionSettings(session.Id,
			session.Name,
			session.ScheduledDate
		);
	}
}
