using Concerto.Server.Extensions;

namespace Concerto.Server.Data.Models;

public class Course : Entity
{
	public string Name { get; set; }
	public string Description { get; set; } = string.Empty;
	public long OwnerId { get; set; }

	public DateTime CreatedDate { get; set; }
	public virtual ICollection<CourseUser> CourseUsers { get; set; } = null!;

	public long ConversationId { get; set; }
	public Conversation Conversation { get; set; }

    public virtual ICollection<Session> Sessions { get; set; } = null!;

    public long? RootFolderId { get; set; }
    public Folder? RootFolder { get; set; } = null!;
}

public static partial class ViewModelConversions
{
    public static Dto.Course ToViewModel(this Course course)
    {
        return new Dto.Course
        (
            Id: course.Id,
            Description: course.Description,
            Name: course.Name,
			RootFolderId: course.RootFolderId!.Value,
			ConversationId: course.ConversationId
        );
    }

	public static Dto.CourseListItem ToCourseListItem(this Course course)
	{
		return new Dto.CourseListItem(course.Id, course.Name, course.Description, course.CreatedDate);
	}
}