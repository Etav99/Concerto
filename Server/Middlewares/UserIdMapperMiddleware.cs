using Concerto.Shared.Extensions;

namespace Concerto.Server.Middlewares;

public class UserIdMapperMiddleware
{
	private readonly RequestDelegate _next;

	public UserIdMapperMiddleware(RequestDelegate next)
	{
		_next = next;
	}

	public async Task InvokeAsync(HttpContext httpContext)
	{
		if (httpContext.User.Identity?.IsAuthenticated ?? false)
			httpContext.Items["AppUserId"] = httpContext.User.GetSubjectId();

		await _next(httpContext);
	}
}

public static class IdAssignmentMiddlewareExtensions
{
	public static IApplicationBuilder UseUserIdMapperMiddleware(
		this IApplicationBuilder builder
	)
	{
		return builder.UseMiddleware<UserIdMapperMiddleware>();
	}

	public static Guid UserId(this HttpContext context)
	{
		return (Guid)context.Items["AppUserId"]!;
	}
}

