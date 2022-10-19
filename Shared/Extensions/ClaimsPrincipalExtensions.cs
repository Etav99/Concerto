using System.Security.Claims;

namespace Concerto.Shared.Extensions;

public static class ClaimsPrincipalExtensions
{
	public static long? GetUserId(this ClaimsPrincipal user)
	{
		return user.FindFirst("user_id")?.Value.ToUserId();
	}

	public static Guid GetSubjectId(this ClaimsPrincipal user)
	{
        var subId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value.ToString();
        return subId is not null ? Guid.Parse(subId) : Guid.Empty;
    }
    
    public static string GetUsername(this ClaimsPrincipal user)
    {
        return user.FindFirst("preferred_username")?.Value.ToString() ?? string.Empty;
    }
    public static string GetFirstName(this ClaimsPrincipal user)
    {
        return user.FindFirst(ClaimTypes.GivenName)?.Value.ToString() ?? string.Empty;
    }
    public static string GetLastName(this ClaimsPrincipal user)
    {
        return user.FindFirst(ClaimTypes.Surname)?.Value.ToString() ?? string.Empty;
    }

    public static string? GetUserIdString(this ClaimsPrincipal user)
	{
		return user.FindFirst("user_id")?.Value;
	}
}