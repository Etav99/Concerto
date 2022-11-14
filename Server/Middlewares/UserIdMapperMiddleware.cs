using Concerto.Server.Services;
using Concerto.Shared.Extensions;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Concerto.Server.Middlewares;

public class UserIdMapperMiddleware
{
    private readonly RequestDelegate _next;
    public UserIdMapperMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext httpContext, UserService userService)
    {
        if(httpContext.User.Identity?.IsAuthenticated ?? false)
        {
            var UserId = await userService.GetUserId(httpContext.User.GetSubjectId());
            if(UserId == null)
            {
                await userService.AddUserIfNotExists(httpContext.User);
                UserId = await userService.GetUserId(httpContext.User.GetSubjectId());
            }
            
            httpContext.Items["AppUserId"] = UserId;
        }
        await _next(httpContext);
    }
}

public static class IdAssignmentMiddlewareExtensions
{
    public static IApplicationBuilder UseUserIdMapperMiddleware(
        this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<UserIdMapperMiddleware>();
    }
    
    public static long GetUserId(this HttpContext context)
    {
        return (long)context.Items["AppUserId"]!;
    }
}
