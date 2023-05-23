using System.Security.Claims;

namespace Concerto.Shared.Extensions;

public static class ClaimsPrincipalExtensions
{
	public static bool IsAdmin(this ClaimsPrincipal user)
	{
		return user.IsInRole("admin");
	}

	public static bool IsTeacher(this ClaimsPrincipal user)
	{
		return user.IsInRole("teacher");
	}

	public static bool IsStudent(this ClaimsPrincipal user)
	{
		return user.IsInRole("user");
	}

	public static bool IsVerified(this ClaimsPrincipal user)
	{
		return !user.IsInRole("unverified");
	}

	public static Guid GetSubjectId(this ClaimsPrincipal user)
	{
		var subId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
		return subId is not null ? Guid.Parse(subId) : Guid.Empty;
	}

	public static string GetUsername(this ClaimsPrincipal user)
	{
		return user.FindFirst("preferred_username")?.Value ?? string.Empty;
	}

	public static string GetFirstName(this ClaimsPrincipal user)
	{
		return user.FindFirst(ClaimTypes.GivenName)?.Value ?? string.Empty;
	}

	public static string GetLastName(this ClaimsPrincipal user)
	{
		return user.FindFirst(ClaimTypes.Surname)?.Value ?? string.Empty;
	}

	public static string? GetUserIdString(this ClaimsPrincipal user)
	{
		var guid = user.GetSubjectId();
		return guid == Guid.Empty ? null : guid.ToString();
	}
}


