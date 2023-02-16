using Concerto.Server.Services;
using Concerto.Shared.Extensions;
using Nito.AsyncEx;

namespace Concerto.Server.Middlewares;

public class UserIdMapperMiddleware
{
	private readonly RequestDelegate _next;
	private readonly AsyncLock _mutex = new();

	public UserIdMapperMiddleware(RequestDelegate next)
	{
		_next = next;
	}

	public async Task InvokeAsync(HttpContext httpContext, UserService userService)
	{
		if (httpContext.User.Identity?.IsAuthenticated ?? false && httpContext.User.IsVerified())
		{
			using (await _mutex.LockAsync())
			{
				var userId = await userService.GetUserIdAndUpdate(httpContext.User);
				httpContext.Items["AppUserId"] = userId;
			}
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

