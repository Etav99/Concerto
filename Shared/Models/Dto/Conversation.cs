namespace Concerto.Shared.Models.Dto;
public record Conversation : EntityDto
{
    public bool IsPrivate { get; init; }
    public IEnumerable<Dto.User>? Users { get; init; }
    public Dto.ChatMessage? LastMessage { get; set; }
}
