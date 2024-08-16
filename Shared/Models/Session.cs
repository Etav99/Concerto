namespace Concerto.Shared.Models.Dto;

public record Session(
	long Id,
	string Name,
	DateTime ScheduledDateTime,
	long WorkspaceId,
	string WorkspaceName,
	long WorkspaceRootFolderId,
	long FolderId,
	bool CanManage,
	Guid MeetingGuid
) : EntityModel(Id);

public record SessionListItem(
	long Id,
	string Name,
	DateTime ScheduledDate,
	long FolderId
) : EntityModel(Id);

public record SessionSettings(
	long Id,
	string Name,
	DateTime ScheduledDate
) : EntityModel(Id);

public record CreateSessionRequest
{
	public string Name { get; set; } = null!;
	public DateTime ScheduledDateTime { get; set; }
	public long WorkspaceId { get; set; }
}

public record UpdateSessionRequest
{
	public long SessionId { get; set; }
	public string Name { get; set; } = null!;
	public DateTime ScheduledDateTime { get; set; }
}


