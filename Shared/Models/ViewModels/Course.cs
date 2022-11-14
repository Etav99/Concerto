namespace Concerto.Shared.Models.Dto;

public record Course : EntityModel
{
	public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public long OwnerId { get; set; }
	public IEnumerable<Dto.User> Users { get; set; } = null!;
    public Dto.Conversation Conversation { get; set; } = null!;
    public IEnumerable<Dto.Session> Sessions { get; set; } = null!;
    public long RootFolderId { get; set; }
}

public record CourseUser
{
    public long UserId { get; set; }
    public CourseUserRole Role { get; set; }
}
public enum CourseUserRole
{
    Admin = 0,
    Supervisor = 1,
    Member = 2,
}

public record CreateCourseRequest
{
    public string Name { get; set; } = null!;
    public string Description { get; set; } = string.Empty;
    public IEnumerable<Dto.CourseUser> Members { get; set; } = null!;
}