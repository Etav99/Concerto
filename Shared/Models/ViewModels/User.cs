using System.Diagnostics.CodeAnalysis;

namespace Concerto.Shared.Models.Dto;

public record User
{
	public long Id { get; init; }
	public string Username { get; init; }
	public string FirstName { get; init; }
	public string LastName { get; init; }

	public string FullName
	{
		get
		{
			return $"{FirstName} {LastName}";
		}
	}
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