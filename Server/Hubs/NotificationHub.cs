using Concerto.Server.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Concerto.Server.Hubs;

[Authorize]
public class NotificationHub : Hub
{
	private readonly WorkspaceService _workspaceService;
	private readonly ForumService _forumService;

	private readonly ILogger<NotificationHub> _logger;
	private readonly StorageService _storageService;
	private readonly UserService _userService;

	public NotificationHub(
		ILogger<NotificationHub> logger,
		ForumService chatService,
		UserService userService,
		WorkspaceService workspaceService,
		StorageService storageService
	)
	{
		_logger = logger;
		_forumService = chatService;
		_userService = userService;
		_workspaceService = workspaceService;
		_storageService = storageService;
	}

	private long? UserId => (long?)Context.GetHttpContext()?.Items["AppUserId"];

	public static string ForumGroup(long workspaceId)
	{
		return $"Forum-{workspaceId}";
	}

	public static string FolderGroup(long workspaceId)
	{
		return $"Folder-{workspaceId}";
	}

	public override async Task OnConnectedAsync()
	{
		await base.OnConnectedAsync();
	}


	public async Task SubscribeForum(long workspaceId)
	{
		await Groups.AddToGroupAsync(Context.ConnectionId, ForumGroup(workspaceId));
	}

	public async Task UnsubscribeForum(long workspaceId)
	{
		await Groups.RemoveFromGroupAsync(Context.ConnectionId, ForumGroup(workspaceId));
	}
}

