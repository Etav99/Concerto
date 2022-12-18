using Concerto.Shared.Models.Dto;

namespace Concerto.Server.Data.Models;

public class Course : Entity
{
	public string Name { get; set; } = string.Empty;
	public string Description { get; set; } = string.Empty;
	public long OwnerId { get; set; }

	public DateTime CreatedDate { get; set; }
	public virtual ICollection<CourseUser> CourseUsers { get; set; } = null!;

	public ICollection<Post> Posts { get; set; } = null!;

	public virtual ICollection<Session> Sessions { get; set; } = null!;

	public long? RootFolderId { get; set; }
	public Folder? RootFolder { get; set; } = null!;
}

public static partial class ViewModelConversions
{
	public static Dto.Course ToViewModel(this Course course, bool canManage)
	{
		return new Dto.Course(course.Id,
			Description: course.Description,
			Name: course.Name,
			RootFolderId: course.RootFolderId!.Value,
			CanManage: canManage
		);
	}

	public static CourseSettings ToSettingsViewModel(this Course course, long userId, CourseUserRole currentUserRole, bool canManage)
	{
		return new CourseSettings(course.Id,
			Description: course.Description,
			Name: course.Name,
			Members: course.CourseUsers.Where(cu => cu.UserId != userId).Select(cu => cu.ToViewModel()),
			CurrentUserRole: currentUserRole.ToViewModel(),
			CanManage: canManage
		);
	}

	public static CourseListItem ToCourseListItem(this Course course)
	{
		return new CourseListItem(course.Id, course.Name, course.Description, course.CreatedDate);
	}
}
