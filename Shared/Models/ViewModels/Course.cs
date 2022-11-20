namespace Concerto.Shared.Models.Dto;

public record Course(
    long Id,
    string Name,
    string Description,
    long ConversationId,
    long RootFolderId
) : EntityModel(Id);

public record CourseListItem(long Id, string Name, string Description, DateTime CreatedDate) : EntityModel(Id);

public record CourseUser(long UserId, CourseUserRole Role)
{
    public CourseUserRole Role { get; set; } = Role;
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