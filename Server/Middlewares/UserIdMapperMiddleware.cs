using Concerto.Server.Services;
using Nito.AsyncEx;

namespace Concerto.Server.Middlewares;

public class UserIdMapperMiddleware
{
	private readonly RequestDelegate _next;
	private readonly AsyncLock mutex = new();

	public UserIdMapperMiddleware(RequestDelegate next)
	{
		_next = next;
	}

	public async Task InvokeAsync(HttpContext httpContext, UserService userService)
	{
		if (httpContext.User.Identity?.IsAuthenticated ?? false)
			using (await mutex.LockAsync())
			{
				var UserId = await userService.GetUserIdAndUpdate(httpContext.User);
				httpContext.Items["AppUserId"] = UserId;
			}

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

	public static long UserId(this HttpContext context)
	{
		return (long)context.Items["AppUserId"]!;
	}
}

