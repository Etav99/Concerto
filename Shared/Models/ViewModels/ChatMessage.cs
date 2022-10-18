namespace Concerto.Shared.Models.Dto;

public record ChatMessage
{
	public DateTime SendTimestamp { get; init; }
	public long SenderId { get; init; }
	public long ConversationId { get; init; }
	public string Content { get; init; }
}
