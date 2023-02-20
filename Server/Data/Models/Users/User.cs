using Concerto.Shared.Extensions;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace Concerto.Server.Data.Models;

public class User
{
	public User() { }

	public User(ClaimsPrincipal claimsPrincipal)
	{
		Id = claimsPrincipal.GetSubjectId();
		Username = claimsPrincipal.GetUsername();
		FirstName = claimsPrincipal.GetFirstName();
		LastName = claimsPrincipal.GetLastName();
	}

	public Guid Id { get; set; }

	[Required] public string Username { get; set; } = null!;

	public string FirstName { get; set; } = null!;
	public string LastName { get; set; } = null!;

	public string FullName => $"{FirstName} {LastName}";
}

public static partial class ViewModelConversions
{
	public static Dto.User ToViewModel(this User user)
	{
		return new Dto.User(user.Id, user.Username, user.FirstName, user.LastName);
	}

	public static IEnumerable<Dto.User> ToViewModel(this IEnumerable<User>? users)
	{
		if (users == null)
			return Enumerable.Empty<Dto.User>();
		return users.Select(c => c.ToViewModel());
	}
}
