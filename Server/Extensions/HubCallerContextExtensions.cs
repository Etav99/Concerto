using System.Security.Claims;

namespace Concerto.Server.Extensions;

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

}