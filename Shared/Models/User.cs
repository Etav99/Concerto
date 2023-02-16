using System.Diagnostics.CodeAnalysis;

namespace Concerto.Shared.Models.Dto;

public record User (long Id, string Username, string FirstName, string LastName)
{
	public string FullName => $"{FirstName} {LastName}";
	public string Initials => $"{FirstName.FirstOrDefault()}{LastName.FirstOrDefault()}";
}

public record UserIdentity(Guid SubjectId, string Username, string FirstName, string LastName, string Email, bool EmailVerified, Role Role)
{
	public string FullName => $"{FirstName} {LastName}";
}


public class UserIdEqualityComparer : IEqualityComparer<User>
{
	public bool Equals(User? x, User? y)
	{
		return x?.Id == y?.Id;
	}

	public int GetHashCode([DisallowNull] User obj)
	{
		return obj.Id.GetHashCode();
	}
}

public enum Role
{
	Unverified,
	User,
	Teacher,
	Admin,
}