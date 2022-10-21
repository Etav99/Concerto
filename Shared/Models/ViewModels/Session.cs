namespace Concerto.Shared.Models.Dto;
public record Session : EntityModel
{
	public string Name { get; init; }
    public long RoomId { get; init; }
    public long RoomOwnerId { get; init; }
    public DateTime ScheduledDateTime { get; set; }
	public Conversation Conversation { get; set; }
	public IEnumerable<Dto.UploadedFile>? Files { get; set; }
	public Guid MeetingGuid { get; init; }
}

public record CreateSessionRequest
{
	public string Name { get; set; }
	public DateTime ScheduledDateTime { get; set; }
	public long RoomId { get; set; }
}