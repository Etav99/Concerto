using Concerto.Server.Data.DatabaseContext;
using Concerto.Server.Data.Models;
using Concerto.Shared.Models.Dto;
using Microsoft.EntityFrameworkCore;
using Comment = Concerto.Shared.Models.Dto.Comment;
using WorkspaceUserRole = Concerto.Server.Data.Models.WorkspaceUserRole;
using Post = Concerto.Shared.Models.Dto.Post;

namespace Concerto.Server.Services;

public class ForumService
{
	private readonly ConcertoDbContext _context;
	private readonly WorkspaceService _workspaceService;
	private readonly ILogger<ForumService> _logger;

	public ForumService(ILogger<ForumService> logger, ConcertoDbContext context, WorkspaceService workspaceService)
	{
		_context = context;
		_logger = logger;
		_workspaceService = workspaceService;
	}

	internal async Task<Post?> CreatePost(CreatePostRequest request, Guid userId)
	{
		var referencedFiles = await _context.UploadedFiles
			.Where(rf => request.ReferencedFilesIds.Contains(rf.Id))
			.ToListAsync();

		var post = new Data.Models.Post
		{
			WorkspaceId = request.WorkspaceId,
			AuthorId = userId,
			Title = request.Title,
			Content = request.Content,
			CreatedAt = DateTime.UtcNow,
			ReferencedFiles = referencedFiles,
			Edited = false
		};

		await _context.Posts.AddAsync(post);
		await _context.SaveChangesAsync();

		await _context.Entry(post).Reference(p => p.Author).LoadAsync();

		return post.ToViewModel(0, true, true);
	}

	internal async Task<IEnumerable<Post>> GetPosts(long workspaceId, Guid userId, bool isAdmin = false, long? beforeId = null, long? relatedToFileId = null)
	{
		const int pageSize = 10;

		var query = _context.Posts
			.Where(p => p.WorkspaceId == workspaceId);

		if (beforeId != null) query = query.Where(p => p.Id < beforeId);
		if (relatedToFileId != null)
			query = query.Where(p => p.ReferencedFiles.Any(rf => rf.Id == relatedToFileId));

		var posts = await query
			.OrderByDescending(p => p.Id)
			.Take(pageSize)
			.Include(p => p.Author)
			.Include(p => p.ReferencedFiles)
			.ToListAsync();


		var postsViewModels = posts
			.Select(async p => p.ToViewModel(await GetPostCommentsCount(p.Id), await CanEditPost(userId, p.Id),
					isAdmin || await CanDeletePost(userId, p.Id)
				)
			)
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

	internal async Task<Post?> UpdatePost(EditPostRequest request, Guid userId)
	{
		var post = await _context.Posts.FindAsync(request.PostId);
		if (post == null) return null;

		await _context.Entry(post).Collection(p => p.ReferencedFiles).LoadAsync();

		var referencedFiles = await _context.UploadedFiles
			.Where(rf => request.ReferencedFilesIds.Contains(rf.Id))
			.ToListAsync();

		post.ReferencedFiles = referencedFiles;
		post.Title = request.Title;
		post.Content = request.Content;
		post.Edited = true;

		var commentsCount = await _context.Entry(post).Collection(p => p.Comments).Query().CountAsync();

		await _context.SaveChangesAsync();
		await _context.Entry(post).Reference(p => p.Author).LoadAsync();
		return post.ToViewModel(commentsCount, true, true);
	}

	internal async Task DeletePost(long postId)
	{
		var post = await _context.Posts.FindAsync(postId);
		if (post == null) return;

		_context.Posts.Remove(post);
		await _context.SaveChangesAsync();
	}

	internal async Task<Comment?> CreateComment(CreateCommentRequest request, Guid userId)
	{
		var comment = new Data.Models.Comment
		{
			PostId = request.PostId,
			AuthorId = userId,
			Content = request.Content,
			CreatedAt = DateTime.UtcNow,
			Edited = false
		};

		await _context.Comments.AddAsync(comment);
		await _context.SaveChangesAsync();

		await _context.Entry(comment).Reference(c => c.Author).LoadAsync();
		return comment.ToViewModel(true, true);
	}

	internal async Task<IEnumerable<Comment>> GetComments(long postId, Guid userId, bool isAdmin = false, long? beforeId = null)
	{
		const int pageSize = 5;

		var query = _context.Comments
			.Where(c => c.PostId == postId);

		if (beforeId != null) query = query.Where(c => c.Id < beforeId);

		var comments = await query
			.OrderByDescending(c => c.Id)
			.Take(pageSize)
			.Include(c => c.Author)
			.ToListAsync();

		return comments
			.Select(async c => c.ToViewModel(await CanEditComment(c.Id, userId), isAdmin || await CanDeleteComment(c.Id, userId)))
			.Select(t => t.Result);
	}

	internal async Task<Comment?> UpdateComment(EditCommentRequest request)
	{
		var comment = await _context.Comments.FindAsync(request.CommentId);
		if (comment == null) return null;

		comment.Content = request.Content;
		comment.Edited = true;
		await _context.SaveChangesAsync();
		await _context.Entry(comment).Reference(c => c.Author).LoadAsync();
		return comment.ToViewModel(true, true);
	}

	internal async Task DeleteComment(long commentId)
	{
		var comment = await _context.Comments.FindAsync(commentId);
		if (comment == null) return;

		_context.Comments.Remove(comment);
		await _context.SaveChangesAsync();
	}

	internal async Task<bool> CanCommentPost(Guid userId, long postId)
	{
		var post = await _context.Posts.FindAsync(postId);
		if (post == null) return false;

		return await _workspaceService.IsUserWorkspaceMember(userId, post.WorkspaceId);
	}

	internal async Task<bool> CanEditPost(Guid userId, long postId)
	{
		var post = await _context.Posts.FindAsync(postId);
		if (post == null) return false;

		return post.AuthorId == userId;
	}

	internal async Task<bool> CanDeletePost(Guid userId, long postId)
	{
		var post = await _context.Posts.FindAsync(postId);
		if (post == null) return false;

		if (post.AuthorId == userId) return true;

		var workspaceRole = (await _context.WorkspaceUsers.FindAsync(post.WorkspaceId, userId))?.Role;

		return workspaceRole is WorkspaceUserRole.Admin or WorkspaceUserRole.Supervisor;
	}

	internal async Task<bool> CanEditComment(long commentId, Guid userId)
	{
		var comment = await _context.Comments.FindAsync(commentId);
		if (comment == null) return false;

		return comment.AuthorId == userId;
	}

	internal async Task<bool> CanDeleteComment(long commentId, Guid userId)
	{
		var comment = await _context.Comments.FindAsync(commentId);
		if (comment == null) return false;

		if (comment.AuthorId == userId) return true;

		var post = await _context.Posts.FindAsync(comment.PostId);
		if (post == null) return false;

		var workspaceRole = (await _context.WorkspaceUsers.FindAsync(post.WorkspaceId, userId))?.Role;

		return workspaceRole is WorkspaceUserRole.Admin or WorkspaceUserRole.Supervisor;
	}

	internal async Task<bool> CanAccessPost(Guid userId, long postId)
	{
		var post = await _context.Posts.FindAsync(postId);
		if (post == null) return false;

		return await _workspaceService.IsUserWorkspaceMember(userId, post.WorkspaceId);
	}
}


