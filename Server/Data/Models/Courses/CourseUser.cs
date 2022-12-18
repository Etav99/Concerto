namespace Concerto.Server.Data.Models;

public class CourseUser
{
	public long CourseId { get; set; }
	public Course Course { get; set; } = null!;
	public long UserId { get; set; }
	public User User { get; set; } = null!;
	public CourseUserRole Role { get; set; }
}

public enum CourseUserRole
{
	Admin = 0,
	Supervisor = 1,
	Member = 2
}

public static partial class ViewModelConversions
{
	public static Dto.CourseUser ToViewModel(this CourseUser role)
	{
		return new Dto.CourseUser(role.UserId, role.Role.ToViewModel());
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
