using Concerto.Shared.Extensions;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace Concerto.Server.Data.Models;

[Index(nameof(SubjectId), IsUnique = true)]
public class User : Entity
{
	public Guid SubjectId { get; set; }
	[Required]
    public string Username { get; set; } = null!;
    public string FirstName { get; set; } = null!;
    public string LastName { get; set; } = null!;
	public virtual ICollection<CourseUser> CoursesUser { get; set; } = null!;

	public string FullName => $"{FirstName} {LastName}";

	public User() { }

	public User(ClaimsPrincipal claimsPrincipal)
	{
		SubjectId = claimsPrincipal.GetSubjectId();
		Username = claimsPrincipal.GetUsername();
		FirstName = claimsPrincipal.GetFirstName();
		LastName = claimsPrincipal.GetLastName();
	}
}


public static partial class ViewModelConversions
{
	public static Dto.User ToViewModel(this User user)
	{
		return new Dto.User
		{
			Id = user.Id,
			Username = user.Username,
			FirstName = user.FirstName,
			LastName = user.LastName
		};
	}

	public static IEnumerable<Dto.User> ToViewModel(this IEnumerable<User>? users)
	{
		if (users == null)
			return Enumerable.Empty<Dto.User>();
		return users.Select(c => c.ToViewModel());
	}

}