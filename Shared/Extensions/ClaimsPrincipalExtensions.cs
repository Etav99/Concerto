using System.Security.Claims;

namespace Concerto.Shared.Extensions;

public static class ClaimsPrincipalExtensions
{
	public static long? GetUserId(this ClaimsPrincipal user)
	{
		return user.FindFirst("user_id")?.Value.ToUserId();
	}

	public static string? GetUserIdString(this ClaimsPrincipal user)
	{
		return user.FindFirst("user_id")?.Value;
	}
}