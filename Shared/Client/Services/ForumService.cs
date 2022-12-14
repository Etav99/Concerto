using Concerto.Shared.Models.Dto;

namespace Concerto.Shared.Client.Services;

public interface IForumService
{
	public Task<IEnumerable<Post>> GetPosts(long sourceId, long? beforeId = null);

	public Task<IEnumerable<Comment>> GetComments(long postId, long? beforeId = null);

	public Task<Post> CreatePost(CreatePostRequest request);
	public Task<Comment> CreateComment(CreateCommentRequest request);

	public Task EditPost(EditPostRequest request);
	public Task EditComment(EditCommentRequest request);

	public Task DeletePost(long postId);
	public Task DeleteComment(long commentId);
}

public class ForumService : IForumService
{
	private readonly IForumClient _forumClient;

	public ForumService(IForumClient forumClient)
	{
		_forumClient = forumClient;
	}
	
	public async Task<IEnumerable<Post>> GetPosts(long sourceId, long? beforeId) => await _forumClient.GetPostsAsync(sourceId, beforeId);
	public async Task<IEnumerable<Comment>> GetComments(long postId, long? beforeId) => await _forumClient.GetCommentsAsync(postId, beforeId);

	public async Task<Comment> CreateComment(CreateCommentRequest request) => await _forumClient.CreateCommentAsync(request);
	public async Task<Post> CreatePost(CreatePostRequest request) => await _forumClient.CreatePostAsync(request);
	public async Task DeleteComment(long commentId) => await _forumClient.DeleteCommentAsync(commentId);

	public async Task DeletePost(long postId) => await _forumClient.DeletePostAsync(postId);

	public async Task EditComment(EditCommentRequest request) => await _forumClient.UpdateCommentAsync(request);

	public async Task EditPost(EditPostRequest request) => await _forumClient.UpdatePostAsync(request);
}
