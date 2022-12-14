using Concerto.Client.Services;
using Concerto.Server.Data.DatabaseContext;
using Concerto.Server.Data.Models;
using Concerto.Server.Extensions;
using Concerto.Server.Hubs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace Concerto.Server.Services;

public class ForumService
{
	private readonly ILogger<ForumService> _logger;
	private readonly AppDataContext _context;
	private readonly CourseService _courseService;

	public ForumService(ILogger<ForumService> logger, AppDataContext context, CourseService courseService)
	{
		_context = context;
		_logger = logger;
		_courseService = courseService;
	}

	internal async Task<Dto.Post?> CreatePost(Dto.CreatePostRequest request, long userId)
	{
		var post = new Post
		{
			CourseId = request.CourseId,
			AuthorId = userId,
			Title = request.Title,
			Content = request.Content,
			CreatedAt = DateTime.UtcNow,
			Edited = false,
		};
		
		await _context.Posts.AddAsync(post);
		await _context.SaveChangesAsync();
		return post.ToViewModel(0, true, true);
	}
	
	internal async Task<IEnumerable<Dto.Post>> GetPosts(long courseId, long userId, bool isAdmin = false, long? beforeId = null)
	{
		const int pageSize = 10;

		var query = _context.Posts
			.Where(p => p.CourseId == courseId);

		if (beforeId != null)
		{
			query = query.Where(p => p.Id < beforeId);
		}

		var posts = await query
			.OrderByDescending(p => p.Id)
			.Take(pageSize)
			.Include(p => p.Author)
			.ToListAsync();


		var postsViewModels = posts
			.Select(async (p) => p.ToViewModel(await GetPostCommentsCount(p.Id), await CanEditPost(userId, p.Id), isAdmin ? true : await CanDeletePost(userId, p.Id)))
			.Select(t => t.Result)
			.ToList();

		foreach (var post in postsViewModels)
		{
			var postComments = await GetComments(post.Id, userId, isAdmin);
			post.Comments.AddRange(postComments);
		}

		return postsViewModels;
	}

	internal async Task<int> GetPostCommentsCount(long postId)
	{
		return await _context.Comments
			.Where(c => c.PostId == postId)
			.CountAsync();
	}

	internal async Task<Dto.Post?> UpdatePost(Dto.EditPostRequest request, long userId)
	{
		Post? post = await _context.Posts.FindAsync(request.PostId);
		if (post == null) return null;

		post.Title = request.Title;
		post.Content = request.Content;
		post.Edited = true;

		int commentsCount = await _context.Entry(post).Collection(p => p.Comments).Query().CountAsync();

		await _context.SaveChangesAsync();
		return post.ToViewModel(commentsCount, true, true);
	}

	internal async Task DeletePost(long postId)
	{
		Post? post = await _context.Posts.FindAsync(postId);
		if (post == null) return;

		_context.Posts.Remove(post);
		await _context.SaveChangesAsync();
	}

	internal async Task<Dto.Comment?> CreateComment(Dto.CreateCommentRequest request, long userId)
	{
		var comment = new Comment
		{
			PostId = request.PostId,
			AuthorId = userId,
			Content = request.Content,
			CreatedAt = DateTime.UtcNow,
			Edited = false,
		};

		await _context.Comments.AddAsync(comment);
		await _context.SaveChangesAsync();
		return comment.ToViewModel(true, true);
	}
	
	internal async Task<IEnumerable<Dto.Comment>> GetComments(long postId, long userId, bool isAdmin = false, long? beforeId = null)
	{
		const int pageSize = 5;

		var query = _context.Comments
			.Where(c => c.PostId == postId);

		if (beforeId != null)
		{
			query = query.Where(c => c.Id < beforeId);
		}

		var comments =  await query
			.OrderByDescending(c => c.Id)
			.Take(pageSize)
			.Include(c => c.Author)
			.ToListAsync();

		return comments
			.Select(async (c) => c.ToViewModel(await CanEditComment(c.Id, userId), isAdmin ? true : await CanDeleteComment(c.Id, userId)))
			.Select(t => t.Result);
	}

	internal async Task<Dto.Comment?> UpdateComment(Dto.EditCommentRequest request, long userId)
	{
		Comment? comment = await _context.Comments.FindAsync(request.CommentId);
		if (comment == null) return null;
		
		comment.Content = request.Content;
		comment.Edited = true;
		await _context.SaveChangesAsync();
		return comment.ToViewModel(true, true);
	}

	internal async Task DeleteComment(long commentId)
	{
		Comment? comment = await _context.Comments.FindAsync(commentId);
		if (comment == null) return;

		_context.Comments.Remove(comment);
		await _context.SaveChangesAsync();
	}

	internal async Task<bool> CanCommentPost(long userId, long postId)
	{
		Post? post = await _context.Posts.FindAsync(postId);
		if (post == null) return false;

		return await _courseService.IsUserCourseMember(userId, post.CourseId);
	}

	internal async Task<bool> CanEditPost(long userId, long postId)
	{
		var post = await _context.Posts.FindAsync(postId);
		if (post == null) return false;

		return post.AuthorId == userId;
	}

	internal async Task<bool> CanDeletePost(long userId, long postId)
	{
		var post = await _context.Posts.FindAsync(postId);
		if (post == null) return false;

		if (post.AuthorId == userId) return true;

		var courseRole = (await _context.CourseUsers.FindAsync(post.CourseId, userId))?.Role;
		if (courseRole == null) return false;

		return courseRole == CourseUserRole.Admin || courseRole == CourseUserRole.Supervisor;
	}

	internal async Task<bool> CanEditComment(long commentId, long userId)
	{
		var comment = await _context.Comments.FindAsync(commentId);
		if (comment == null) return false;

		return comment.AuthorId == userId;
	}

	internal async Task<bool> CanDeleteComment(long commentId, long userId)
	{
		var comment = await _context.Comments.FindAsync(commentId);
		if (comment == null) return false;

		if (comment.AuthorId == userId) return true;

		var post = await _context.Posts.FindAsync(comment.PostId);
		if (post == null) return false;

		var courseRole = (await _context.CourseUsers.FindAsync(post.CourseId, userId))?.Role;
		if (courseRole == null) return false;

		return courseRole == CourseUserRole.Admin || courseRole == CourseUserRole.Supervisor;
	}
	
	internal async Task<bool> CanAccessPost(long userId, long postId)
	{
		var post = await _context.Posts.FindAsync(postId);
		if (post == null) return false;

		return await _courseService.IsUserCourseMember(userId, post.CourseId);
	}
}

