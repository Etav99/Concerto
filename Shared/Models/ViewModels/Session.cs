namespace Concerto.Shared.Models.Dto;
public record Session(
	long Id,
	string Name,
	DateTime ScheduledDateTime,
	long CourseId,
	string CourseName,
	long? CourseRootFolderId,
	bool CanManage,
	Guid MeetingGuid
) : EntityModel(Id);

public record SessionListItem(
	long Id,
	string Name,
	DateTime ScheduledDate
) : EntityModel(Id);

public record SessionSettings(
	long Id,
	string Name,
	DateTime ScheduledDate
) : EntityModel(Id);

public record CreateSessionRequest
{
	public string Name { get; set; }
	public DateTime ScheduledDateTime { get; set; }
	public long CourseId { get; set; }
}

public record UpdateSessionRequest
{
	public long SessionId { get; set; }
	public string Name { get; set; }
	public DateTime ScheduledDateTime { get; set; }
}