namespace Concerto.Shared.Models.Dto;

public record Post(
	long Id,
	long CourseId,
	DateTime CreatedAt,
	User Author,
	bool Edited,
	string Title,
	string Content,
	int CommentsCount,
	List<Comment> Comments,
	List<FileItem> ReferencedFiles,
	bool CanEdit,
	bool CanDelete
)
{
	public int CommentsCount { get; set; } = CommentsCount;
	public bool Edited { get; set; } = Edited;
	public string Title { get; set; } = Title;
	public string Content { get; set; } = Content;

	public List<FileItem> ReferencedFiles { get; set; } = ReferencedFiles;

	public void addComments(Comment comment)
	{
		Comments.Add(comment);
	}

	public void addComments(IEnumerable<Comment> comments)
	{
		var commentsList = comments.ToList();
		Comments.AddRange(commentsList);
	}

	public void deleteComment(Comment comment)
	{
		var commentToDeleteIndex = Comments.FindIndex(c => c.Id == comment.Id);
		if (commentToDeleteIndex >= 0)
		{
			CommentsCount--;
			Comments.RemoveAt(commentToDeleteIndex);
		}
	}
}

public record Comment(
	long Id,
	long PostId,
	User Author,
	DateTime CreatedAt,
	bool Edited,
	string Content,
	bool CanEdit,
	bool CanDelete
)
{
	public bool Edited { get; set; } = Edited;
	public string Content { get; set; } = Content;
}

public record CreatePostRequest(long CourseId)
{
	public string Title { get; set; } = string.Empty;
	public string Content { get; set; } = string.Empty;
	public ICollection<long> ReferencedFilesIds { get; set; } = new List<long>();
}

public record CreateCommentRequest(long PostId)
{
	public string Content { get; set; } = string.Empty;
}

public record EditPostRequest(long PostId, string Title, string Content)
{
	public string Title { get; set; } = Title;
	public string Content { get; set; } = Content;
	public ICollection<long> ReferencedFilesIds { get; set; } = new List<long>();
}

public record EditCommentRequest(long CommentId, string Content)
{
	public string Content { get; set; } = Content;
}


