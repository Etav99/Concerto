using Concerto.Server.Middlewares;
using Concerto.Server.Services;
using Concerto.Shared.Extensions;
using Concerto.Shared.Models.Dto;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Concerto.Server.Controllers;

[Route("[controller]/[action]")]
[ApiController]
[Authorize]
public class WorkspaceController : ControllerBase
{
	private readonly WorkspaceService _workspaceService;
	private readonly ILogger<WorkspaceController> _logger;


	public WorkspaceController(ILogger<WorkspaceController> logger, WorkspaceService workspaceService)
	{
		_logger = logger;
		_workspaceService = workspaceService;
	}

	private Guid UserId => HttpContext.UserId();

	[HttpGet]
	public async Task<ActionResult<IEnumerable<WorkspaceListItem>>> GetCurrentUserWorkspaces()
	{
		var test = this.HttpContext;
		if (User.IsAdmin()) return Ok(await _workspaceService.GetAllWorkspaces());
		return Ok(await _workspaceService.GetUserWorkspacesList(UserId));
	}

	[HttpGet]
	public async Task<ActionResult<Workspace>> GetWorkspace(long workspaceId)
	{
		var isAdmin = User.IsAdmin();
		if (isAdmin || await _workspaceService.IsUserWorkspaceMember(UserId, workspaceId))
		{
			var workspace = await _workspaceService.GetWorkspace(workspaceId, UserId, isAdmin);
			if (workspace == null) return NotFound();
			return Ok(workspace);
		}

		return Forbid();
	}

	[HttpGet]
	public async Task<ActionResult<WorkspaceSettings>> GetWorkspaceSettings(long workspaceId)
	{
		var isAdmin = User.IsAdmin();
		if (isAdmin || await _workspaceService.CanManageWorkspace(workspaceId, UserId))
		{
			var workspaceSettings = await _workspaceService.GetWorkspaceSettings(workspaceId, UserId, isAdmin);
			if (workspaceSettings == null) return NotFound();
			return Ok(workspaceSettings);
		}

		return Forbid();
	}

	[HttpGet]
	public async Task<ActionResult<IEnumerable<User>>> GetWorkspaceUsers(long workspaceId)
	{
		if (User.IsAdmin() || await _workspaceService.IsUserWorkspaceMember(UserId, workspaceId))
		{
			var workspaceUsers = await _workspaceService.GetWorkspaceUsers(workspaceId);
			return Ok(workspaceUsers);
		}

		return Forbid();
	}

	[Authorize(Policy = AuthorizationPolicies.IsModerator.Name)]
	[HttpPost]
	[ProducesResponseType(StatusCodes.Status201Created)]
	[ProducesResponseType(StatusCodes.Status400BadRequest)]
	public async Task<ActionResult<long>> CreateWorkspaceForCurrentUser([FromBody] CreateWorkspaceRequest request)
	{
		var newWorkspaceId = await _workspaceService.CreateWorkspace(request, UserId);
		if (newWorkspaceId > 0) return CreatedAtAction("GetWorkspace", new { workspaceId = newWorkspaceId }, newWorkspaceId);
		return BadRequest();
	}

	[Authorize(Policy = AuthorizationPolicies.IsModerator.Name)]
	[HttpPost]
	public async Task<ActionResult<long>> CloneWorkspace([FromBody] CloneWorkspaceRequest request)
	{
		if (!await _workspaceService.IsUserWorkspaceMember(UserId, request.WorkspaceId)) return Forbid();
		var newWorkspaceId = await _workspaceService.CloneWorkspace(request, UserId);
		if (newWorkspaceId <= 0) BadRequest();
		return Ok(newWorkspaceId);
	}

	[Authorize(Policy = AuthorizationPolicies.IsModerator.Name)]
	[HttpPost]
	public async Task<ActionResult> UpdateWorkspace([FromBody] UpdateWorkspaceRequest request)
	{
		if (!User.IsAdmin() && !await _workspaceService.CanManageWorkspace(request.WorkspaceId, UserId)) return Forbid();

		if (await _workspaceService.UpdateWorkspace(request, UserId)) return Ok();
		return BadRequest();
	}

	[Authorize(Policy = AuthorizationPolicies.IsModerator.Name)]
	[HttpDelete]
	public async Task<ActionResult> DeleteWorkspace(long workspaceId)
	{
		if (!User.IsAdmin() && !await _workspaceService.CanDeleteWorkspace(workspaceId, UserId)) return Forbid();

		if (await _workspaceService.DeleteWorkspace(workspaceId)) return Ok();

		return BadRequest();
	}

	[HttpGet]
	public async Task<ActionResult<bool>> CanManageWorkspaceSessions(long workspaceId)
	{
		return Ok(User.IsAdmin() || await _workspaceService.CanManageWorkspaceSessions(workspaceId, UserId));
	}

}
