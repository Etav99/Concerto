namespace Concerto.Shared.Models.Dto;
public record Session : EntityDto
{    
    public string Name { get; init; }
    public DateTime ScheduledDateTime { get; set; }
    public Conversation Conversation { get; set; }
    public IEnumerable<Dto.UploadedFile>? Files { get; set; }
}

public record CreateSessionRequest
{
    public string Name { get; set; }
    public DateTime ScheduledDateTime { get; set; }
    public long RoomId { get; set; }
}