using Concerto.Shared.Models.Dto;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations.Schema;

namespace Concerto.Server.Data.Models;

[Index(nameof(CourseId))]
public class Session : Entity
{
	public string Name { get; set; } = null!;
	public DateTime ScheduledDate { get; set; }
	public long CourseId { get; set; }
	public Course Course { get; set; } = null!;

	public long FolderId { get; set; }
	public Folder Folder { get; set; } = null!;

	[DatabaseGenerated(DatabaseGeneratedOption.Identity)]
	public Guid MeetingGuid { get; set; }
}

public static partial class ViewModelConversions
{
	public static Dto.Session ToViewModel(this Session session, bool canManage)
	{
		return new Dto.Session(
			session.Id,
			session.Name,
			CourseId: session.CourseId,
			CourseName: session.Course.Name,
			ScheduledDateTime: session.ScheduledDate,
			CourseRootFolderId: session.Course.RootFolderId!.Value,
			FolderId: session.FolderId,
			MeetingGuid: session.MeetingGuid,
			CanManage: canManage
		);
	}

	public static SessionListItem ToSessionListItem(this Session session)
	{
		return new SessionListItem(session.Id,
			session.Name,
			session.ScheduledDate,
			session.FolderId
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
