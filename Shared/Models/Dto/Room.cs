namespace Concerto.Shared.Models.Dto;

public record Room
{
    public long RoomId { get; set; }
    public string Name { get; set; }

    public IEnumerable<Dto.User> Users { get; set; }
    public Dto.Conversation Conversation { get; set; }
    // public IEnumerable<Dto.Session> Sessions { get; set; }
}