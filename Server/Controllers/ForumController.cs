using Concerto.Server.Extensions;
using Concerto.Server.Middlewares;
using Concerto.Server.Services;
using Concerto.Shared.Models.Dto;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Concerto.Server.Controllers;

[Route("[controller]/[action]")]
[ApiController]
[Authorize]
public class ForumController : ControllerBase
{
	private readonly CourseService _courseService;
	private readonly ForumService _forumService;
	private readonly ILogger<ForumController> _logger;

	public ForumController(ILogger<ForumController> logger, ForumService forumService, CourseService courseService)
	{
		_logger = logger;
		_forumService = forumService;
		_courseService = courseService;
	}

	private long UserId => HttpContext.UserId();

	[HttpPost]
	public async Task<ActionResult<IEnumerable<Post>>> GetPosts(long courseId, long? beforeId = null)
	{
		if (!User.IsAdmin() && !await _courseService.IsUserCourseMember(UserId, courseId)) return Forbid();
		return Ok(await _forumService.GetPosts(courseId, UserId, User.IsAdmin(), beforeId));
	}

	[HttpPost]
	public async Task<ActionResult<IEnumerable<Comment>>> GetComments(long postId, long? beforeId = null)
	{
		if (!User.IsAdmin() && !await _forumService.CanAccessPost(UserId, postId)) return Forbid();
		return Ok(await _forumService.GetComments(postId, UserId, User.IsAdmin(), beforeId));
	}

	[HttpPost]
	public async Task<ActionResult<Post>> CreatePost([FromBody] CreatePostRequest request)
	{
		if (!User.IsAdmin() && !await _courseService.IsUserCourseMember(UserId, request.CourseId)) return Forbid();
		var createdPost = await _forumService.CreatePost(request, UserId);
		return createdPost != null ? Ok(createdPost) : BadRequest();
	}

	[HttpPost]
	public async Task<ActionResult<Post>> UpdatePost([FromBody] EditPostRequest request)
	{
		if (!await _forumService.CanEditPost(UserId, request.PostId)) return Forbid();
		var editedPost = await _forumService.UpdatePost(request, UserId);
		return editedPost != null ? Ok(editedPost) : BadRequest();
	}

	[HttpDelete]
	public async Task<ActionResult> DeletePost(long postId)
	{
		if (!User.IsAdmin() && !await _forumService.CanDeletePost(UserId, postId)) return Forbid();
		await _forumService.DeletePost(postId);
		return Ok();
	}

	[HttpPost]
	public async Task<ActionResult<Comment>> CreateComment([FromBody] CreateCommentRequest request)
	{
		if (!User.IsAdmin() && !await _forumService.CanCommentPost(UserId, request.PostId)) return Forbid();
		var createdComment = await _forumService.CreateComment(request, UserId);
		return createdComment != null ? Ok(createdComment) : BadRequest();
	}

	[HttpPost]
	public async Task<ActionResult<Comment>> UpdateComment([FromBody] EditCommentRequest request)
	{
		if (!await _forumService.CanEditComment(UserId, request.CommentId)) return Forbid();
		var editedComment = await _forumService.UpdateComment(request);
		return editedComment != null ? Ok(editedComment) : BadRequest();
	}

	[HttpDelete]
	public async Task<ActionResult> DeleteComment(long commentId)
	{
		if (!User.IsAdmin() && !await _forumService.CanDeleteComment(commentId, UserId)) return Forbid();
		await _forumService.DeleteComment(commentId);
		return Ok();
	}
}

