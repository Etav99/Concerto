namespace Concerto.Shared.Models.Dto;
public record Session(
	long Id,
	string Name,
	DateTime ScheduledDateTime,
	long CourseId,
	string CourseName,
	long? CourseRootFolderId,
	long ConversationId,
	Guid MeetingGuid
) : EntityModel(Id);

public record SessionListItem(
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