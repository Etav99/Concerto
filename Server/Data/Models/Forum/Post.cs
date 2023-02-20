using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace Concerto.Server.Data.Models;

[Index(nameof(CourseId))]
public class Post : Entity
{
	[Required] public Guid AuthorId { get; set; }

	public User Author { get; set; } = null!;

	[Required] public long CourseId { get; set; }

	public Course Course { get; set; } = null!;

	[Required] public DateTime CreatedAt { get; set; }

	[Required] public string Title { get; set; } = null!;

	[Required] public string Content { get; set; } = null!;


	public bool Edited { get; set; } = false;

	public virtual ICollection<Comment> Comments { get; set; } = null!;
	public virtual ICollection<UploadedFile> ReferencedFiles { get; set; } = null!;
}

[Index(nameof(PostId))]
public class Comment : Entity
{
	[Required] public Guid AuthorId { get; set; }

	public User Author { get; set; } = null!;

	[Required] public long PostId { get; set; }

	public Post Post { get; set; } = null!;

	[Required] public DateTime CreatedAt { get; set; }

	public bool Edited { get; set; } = false;

	[Required] public string Content { get; set; } = null!;
}

public enum PostType
{
	CoursePost,
	SessionPost
}

public static partial class ViewModelConversions
{
	public static Dto.Post ToViewModel(this Post post, int CommentsCount, bool canEdit, bool canDelete)
	{
		return new Dto.Post(post.Id,
			Author: post.Author.ToViewModel(),
			CourseId: post.CourseId,
			CreatedAt: post.CreatedAt,
			Title: post.Title,
			Content: post.Content,
			CommentsCount: CommentsCount,
			Edited: post.Edited,
			Comments: new List<Dto.Comment>(),
			ReferencedFiles: post.ReferencedFiles.Select(f => f.ToFileItem(false)).ToList(),
			CanEdit: canEdit,
			CanDelete: canDelete
		);
	}

	public static Dto.Comment ToViewModel(this Comment comment, bool canEdit, bool canDelete)
	{
		return new Dto.Comment(comment.Id,
			comment.PostId,
			comment.Author.ToViewModel(),
			comment.CreatedAt,
			Content: comment.Content,
			Edited: comment.Edited,
			CanEdit: canEdit,
			CanDelete: canDelete
		);
	}
}
