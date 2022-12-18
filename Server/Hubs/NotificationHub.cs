using Concerto.Server.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Concerto.Server.Hubs;

[Authorize]
public class NotificationHub : Hub
{
	private readonly CourseService _courseService;
	private readonly ForumService _forumService;

	private readonly ILogger<NotificationHub> _logger;
	private readonly StorageService _storageService;
	private readonly UserService _userService;

	public NotificationHub(
		ILogger<NotificationHub> logger,
		ForumService chatService,
		UserService userService,
		CourseService courseService,
		StorageService storageService
	)
	{
		_logger = logger;
		_forumService = chatService;
		_userService = userService;
		_courseService = courseService;
		_storageService = storageService;
	}

	private long? UserId => (long?)Context.GetHttpContext()?.Items["AppUserId"];

	public static string ForumGroup(long courseId)
	{
		return $"Forum-{courseId}";
	}

	public static string FolderGroup(long courseId)
	{
		return $"Folder-{courseId}";
	}

	public override async Task OnConnectedAsync()
	{
		await base.OnConnectedAsync();
	}


	public async Task SubscribeForum(long courseId)
	{
		await Groups.AddToGroupAsync(Context.ConnectionId, ForumGroup(courseId));
	}

	public async Task UnsubscribeForum(long courseId)
	{
		await Groups.RemoveFromGroupAsync(Context.ConnectionId, ForumGroup(courseId));
	}
}

