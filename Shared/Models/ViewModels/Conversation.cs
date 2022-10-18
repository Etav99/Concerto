namespace Concerto.Shared.Models.Dto;
public class Conversation
{
	public long Id { get; set; }
	public bool IsPrivate { get; init; }
	public IEnumerable<Dto.User>? Users { get; init; }
	public Dto.ChatMessage? LastMessage { get; set; }
}
