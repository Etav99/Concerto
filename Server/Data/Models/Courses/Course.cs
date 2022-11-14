using Concerto.Server.Extensions;

namespace Concerto.Server.Data.Models;

public class Course : Entity
{
	public string Name { get; set; }
	public string Description { get; set; } = string.Empty;
	public long OwnerId { get; set; }

    public virtual ICollection<CourseUser> CourseUsers { get; set; } = null!;

	public long ConversationId { get; set; }
	public Conversation Conversation { get; set; }

    public virtual ICollection<Session> Sessions { get; set; } = null!;

    public long RootFolderId { get; set; }
    public Folder RootFolder { get; set; } = null!;
}

public static partial class ViewModelConversions
{
    public static Dto.Course ToDto(this Course course)
    {
        return new Dto.Course
        {
            Id = course.Id,
            OwnerId = course.OwnerId,
            Name = course.Name,
            Users = course.CourseUsers.Select(ru => ru.User.ToDto()),
            Conversation = course.Conversation.ToDto(),
            Sessions = course.Sessions?.Select(s => s.ToDto()) ?? Enumerable.Empty<Dto.Session>(),
        };
    }
}