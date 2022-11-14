using Concerto.Server.Data.DatabaseContext;
using Concerto.Server.Data.Models;
using Concerto.Server.Extensions;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace Concerto.Server.Services;

public class CourseService
{
	private readonly ILogger<CourseService> _logger;

	private readonly AppDataContext _context;
	public CourseService(ILogger<CourseService> logger, AppDataContext context)
	{
		_logger = logger;
		_context = context;
	}

    // Create
    public async Task<bool> CreateCourse(Dto.CreateCourseRequest request, long userId)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null) return false;

        var membersRoles = request.Members.ToDictionary(m => m.UserId, m => m.Role);

        var members = await _context.Users
            .Where(u => membersRoles.Keys.Contains(u.Id))
            .ToListAsync();
        
        // Create course
        var course = new Course();
        course.OwnerId = user.Id;
        course.Name = request.Name;

        // Add members to course
        var courseUsers = new List<CourseUser>
        {
            new CourseUser
            {
                User = user,
                Course = course,
                Role = CourseUserRole.Admin
            }
        };
        foreach (var member in members)
		{
			courseUsers.Add(new CourseUser
			{
				User = member,
				Course = course,
				Role = membersRoles[member.Id].ToEntity()
			});
        }

        course.CourseUsers = courseUsers;

        // Create course conversation
        var courseConversation = members.ToGroupConversation();
        course.Conversation = courseConversation;

		// Create course root folder, with default read permissions for course members
		var rootFolder = new Folder
		{
			CoursePermission = new FolderPermission { Inherited = false, Type = FolderPermissionType.Read },
			OwnerId = userId,
			Name = string.Empty,
			Type = FolderType.CourseRoot,
		};
		course.RootFolder = rootFolder;

        await _context.Courses.AddAsync(course);
        await _context.SaveChangesAsync();
        return true;
    }

    // Read
    public async Task<Dto.Course?> GetCourse(long courseId)
	{
		var course = await _context.Courses.FindAsync(courseId);
		if (course == null)
			return null;

		await _context.Entry(course)
			.Collection(r => r.CourseUsers)
			.Query()
			.Include(ru => ru.User)
			.LoadAsync();
		await _context.Entry(course)
			.Reference(r => r.Conversation)
			.Query()
			.Include(c => c.ConversationUsers)
			.ThenInclude(cu => cu.User)
			.LoadAsync();
		await _context.Entry(course)
			.Collection(r => r.Sessions)
			.Query()
			.Include(s => s.Conversation)
			.ThenInclude(c => c.ConversationUsers)
			.LoadAsync();
		return course.ToDto();
	}
    
	public async Task<IEnumerable<Dto.Course>> GetUserCourses(long userId)
	{
		return await _context.Courses
			.Include(r => r.CourseUsers)
			.Where(r => r.CourseUsers.Any(ru => ru.UserId == userId))
			.Include(r => r.CourseUsers)
			.ThenInclude(ru => ru.User)
			.Include(r => r.Conversation)
			.ThenInclude(c => c.ConversationUsers)
			.ThenInclude(cu => cu.User)
			.Select(r => r.ToDto())
			.ToListAsync();
	}

	public async Task<bool> IsUserCourseMember(long userId, long courseId)
	{
		var courseUser = await _context.CourseUsers.FindAsync(courseId, userId);
        return courseUser != null;
	}

    internal async Task<bool> CanDeleteCourse(long courseId, long userId)
    {
        var courseRole = (await _context.CourseUsers.FindAsync(courseId, userId))?.Role;
		return courseRole == CourseUserRole.Admin;
    }

    internal async Task<bool> CanEditCourse(long courseId, long userId)
    {
        var courseRole = (await _context.CourseUsers.FindAsync(courseId, userId))?.Role;
        return courseRole == CourseUserRole.Admin;
    }

	internal async Task<bool> DeleteCourse(long courseId, long userId)
	{
        var course = _context.Courses.Find(courseId);
        if (course == null) return false;

		_context.Remove(course);
        await _context.SaveChangesAsync();
        return true;
    }
}
