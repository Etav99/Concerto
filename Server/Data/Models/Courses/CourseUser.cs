using Microsoft.EntityFrameworkCore;

namespace Concerto.Server.Data.Models;

[Index(nameof(CourseId))]
[Index(nameof(UserId))]
public class CourseUser
{
	public long CourseId { get; set; }
	public Course Course { get; set; } = null!;
	public Guid UserId { get; set; }
	public User User { get; set; } = null!;
	public CourseUserRole Role { get; set; }

	public CourseUser() { }


	public CourseUser(Guid userId, CourseUserRole role)
	{
		CourseId = Entity.NoId;
		UserId = userId;
		Role = role;
	}
	public CourseUser(long courseId, Guid userId, CourseUserRole role)
	{
		CourseId = courseId;
		UserId = userId;
		Role = role;
	}


}

public enum CourseUserRole
{
	Admin = 0,
	Supervisor = 1,
	Member = 2
}

public static partial class ViewModelConversions
{
	public static Dto.CourseUser ToViewModel(this CourseUser courseUser)
	{
		return new Dto.CourseUser(courseUser.UserId, courseUser.Role.ToViewModel());
	}

	public static CourseUser ToEntity(this Dto.CourseUser courseUser, long courseId = 0)
	{
		return new CourseUser
		{
			CourseId = courseId,
			UserId = courseUser.UserId,
			Role = courseUser.Role.ToEntity()
		};
	}

	public static CourseUserRole ToEntity(this Dto.CourseUserRole role)
	{
		switch (role)
		{
			case Dto.CourseUserRole.Admin:
				return CourseUserRole.Admin;
			case Dto.CourseUserRole.Supervisor:
				return CourseUserRole.Supervisor;
			case Dto.CourseUserRole.Member:
				return CourseUserRole.Member;
			default:
				throw new ArgumentOutOfRangeException(nameof(role), role, null);
		}
	}

	public static Dto.CourseUserRole ToViewModel(this CourseUserRole role)
	{
		switch (role)
		{
			case CourseUserRole.Admin:
				return Dto.CourseUserRole.Admin;
			case CourseUserRole.Supervisor:
				return Dto.CourseUserRole.Supervisor;
			case CourseUserRole.Member:
				return Dto.CourseUserRole.Member;
			default:
				throw new ArgumentOutOfRangeException(nameof(role), role, null);
		}
	}
}
