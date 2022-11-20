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
        var membersRoles = request.Members.ToDictionary(m => m.UserId, m => m.Role.ToEntity());
		membersRoles.Add(userId, CourseUserRole.Admin);
		
		var members = await _context.Users
            .Where(u => membersRoles.Keys.Contains(u.Id))
            .ToListAsync();

		if (members.Count != membersRoles.Count)
			return false;

		// Create course
		var course = new Course();
        course.OwnerId = userId;
        course.Name = request.Name;
		course.CreatedDate = DateTime.UtcNow;

		// Add members to course
		var courseUsers = members.Select(member => new CourseUser
		{
			Course = course,
			User = member,
			Role = membersRoles[member.Id]
		}).ToList();
        course.CourseUsers = courseUsers;

        // Create course conversation
        var courseConversation = members.ToGroupConversation();
        course.Conversation = courseConversation;

		await _context.Courses.AddAsync(course);
		await _context.SaveChangesAsync();
		
		// Create course root folder, with default read permissions for course members
		var rootFolder = new Folder
		{
			CoursePermission = new FolderPermission { Inherited = false, Type = FolderPermissionType.Read },
			OwnerId = userId,
			CourseId = course.Id,
			Name = course.Name,
			Type = FolderType.CourseRoot,
		};
		await _context.Folders.AddAsync(rootFolder);
		await _context.SaveChangesAsync();
		
		course.RootFolderId = rootFolder.Id;
		await _context.SaveChangesAsync();

		return true;
    }

    // Read
    public async Task<Dto.Course?> GetCourse(long courseId)
	{
		var course = await _context.Courses.FindAsync(courseId);
		if (course == null)
			return null;

		return course.ToViewModel();
	}
    
	public async Task<IEnumerable<Dto.CourseListItem>> GetUserCoursesList(long userId)
	{
		return await _context.Courses
			.Include(c => c.CourseUsers)
			.Where(c => c.CourseUsers.Any(cu => cu.UserId == userId))
			.Select(c => c.ToCourseListItem())
			.ToListAsync();
	}

    public async Task<IEnumerable<Dto.CourseListItem>> GetAllCourses()
    {
        return await _context.Courses
            .Select(r => r.ToCourseListItem())
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
