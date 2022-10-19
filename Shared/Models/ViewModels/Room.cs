namespace Concerto.Shared.Models.Dto;

public record Room : EntityModel
{
	public string Name { get; set; } = string.Empty;
	public long OwnerId { get; set; }
	public IEnumerable<Dto.User> Users { get; set; }
	public Dto.Conversation Conversation { get; set; }
	public IEnumerable<Dto.Session> Sessions { get; set; }
}

public record CreateRoomRequest
{
	public string Name { get; set; }
	public IEnumerable<Dto.User> Members { get; set; }
}