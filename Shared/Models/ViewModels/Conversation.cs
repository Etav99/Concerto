namespace Concerto.Shared.Models.Dto;
public class Conversation
{
	public long Id { get; set; }
	public bool IsPrivate { get; init; }
	public IEnumerable<Dto.User> Users { get; init; }
	public IList<Dto.ChatMessage> Messages { get; init; }
	
	public string UsersString => string.Join(", ", Users.Select(u => u.FullName) ?? Enumerable.Empty<string>());
}

public record ConversationListItem(long Id, string Name, ChatMessage? LastMessage)
{
	public ChatMessage? LastMessage { get; set; } = LastMessage;
}