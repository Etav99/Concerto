using Concerto.Server.Extensions;
using Concerto.Server.Services;
using Concerto.Shared.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Concerto.Server.Hubs;

[Authorize]
public class NotificationHub : Hub
{

	private readonly ILogger<NotificationHub> _logger;
	private readonly ForumService _forumService;
	private readonly UserService _userService;


	public NotificationHub(ILogger<NotificationHub> logger, ForumService chatService, UserService userService)
	{
		_logger = logger;
		_forumService = chatService;
		_userService = userService;
	}

	public override async Task OnConnectedAsync()
	{
		Guid? userSubId = Context.User?.GetSubjectId();

		if (userSubId is not null)
		{
            var userId = (await _userService.GetUserId(userSubId.Value)).ToString();
            _logger.LogDebug($"User with ID {userId} conncted to ChatHub");
			await Groups.AddToGroupAsync(Context.ConnectionId, userId!);
		}
		else
		{
			throw new Exception("ChatHub connection attempt with empty UserId");
		}
		
		await base.OnConnectedAsync();
	}

}
