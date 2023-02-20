using System.Diagnostics.CodeAnalysis;

namespace Concerto.Shared.Models.Dto;

public record Course(
	long Id,
	string Name,
	string Description,
	long RootFolderId,
	bool CanManage
) : EntityModel(Id);

public record CourseSettings(
	long Id,
	string Name,
	string Description,
	IEnumerable<CourseUser> Members,
	CourseUserRole CurrentUserRole,
	bool CanManage
) : EntityModel(Id);

public record CourseListItem(long Id, string Name, string Description, DateTime CreatedDate) : EntityModel(Id);

public record CourseUser(Guid UserId, CourseUserRole Role)
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
	Member = 2
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
	public IEnumerable<CourseUser> Members { get; set; } = null!;
}

public record UpdateCourseRequest
{
	public long CourseId { get; set; }
	public string Name { get; set; } = null!;
	public string Description { get; set; } = string.Empty;
	public HashSet<CourseUser> Members { get; set; } = null!;
}

public record CloneCourseRequest
{
	public long CourseId { get; set; }
	public string Name { get; set; } = string.Empty;
	public string Description { get; set; } = string.Empty;

	public bool CopyCourseUsers { get; set; } = true;
	public bool CopyRoles { get; set; } = true;

	public bool CopyFolders { get; set; } = true;
	public bool CopyFoldersPermissions { get; set; } = true;
	public bool CopyFiles { get; set; } = false;
}


