namespace Concerto.Shared.Models.Dto;

public record Room : EntityDto
{
    public string Name { get; set; } = string.Empty; 

    public IEnumerable<Dto.User> Users { get; set; } 
    public Dto.Conversation Conversation { get; set; }
    public IEnumerable<Dto.Session> Sessions { get; set; }
}

public record CreateRoomRequest
{
    public string Name { get; set; }
    public IEnumerable<Dto.User> Members { get; set; }
}