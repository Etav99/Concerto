using System.Diagnostics.CodeAnalysis;

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

public class CourseUserIdEqualityComparer : IEqualityComparer<CourseUser>
{
    public bool Equals(CourseUser? x, CourseUser? y)
    {
        return x?.UserId == y?.UserId;
    }

    public int GetHashCode([DisallowNull] CourseUser obj)
    {
        return obj.UserId.GetHashCode();
    }
}

public enum CourseUserRole
{
    Admin = 0,
    Supervisor = 1,
    Member = 2,
}

public static class CourseUserRoleExtensions
{
    public static string ToDisplayString(this CourseUserRole role)
    {
        return role switch
        {
            CourseUserRole.Admin => "Administrator",
            CourseUserRole.Supervisor => "Supervisor",
            CourseUserRole.Member => "Member",
            _ => throw new ArgumentOutOfRangeException(nameof(role), role, null)
        };
    }
}




public record CreateCourseRequest
{
    public string Name { get; set; } = null!;
    public string Description { get; set; } = string.Empty;
    public IEnumerable<Dto.CourseUser> Members { get; set; } = null!;
}

public record UpdateCourseRequest
{
	public long CourseId { get; set; }
	public string Name { get; set; } = null!;
	public string Description { get; set; } = string.Empty;
	public IEnumerable<Dto.CourseUser> Members { get; set; } = null!;
}