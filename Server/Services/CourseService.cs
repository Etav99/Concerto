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
			CourseId = course.Id,
			UserId = member.Id,
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
	public async Task<bool> IsUserCourseMember(long userId, long courseId)
	{
		var courseUser = await _context.CourseUsers.FindAsync(courseId, userId);
		return courseUser != null;
	}
	
	public async Task<Dto.Course?> GetCourse(long courseId, long userId, bool isAdmin = false)
	{
		var course = await _context.Courses.FindAsync(courseId);
		if (course == null)
			return null;
	
		return course.ToViewModel(isAdmin || await CanManageCourse(courseId, userId));
	}

    public async Task<Dto.CourseSettings?> GetCourseSettings(long courseId, long userId, bool isAdmin = false)
    {
        var course = await _context.Courses.FindAsync(courseId);
        if (course == null)
            return null;

		CourseUserRole? courseRole;
		if (isAdmin)
		{
			courseRole = CourseUserRole.Admin;
		}
		else
		{
			courseRole = (await _context.CourseUsers.FindAsync(courseId, userId))?.Role;
			if (courseRole == null)
				return null;
		}
		_context.Entry(course).Collection(c => c.CourseUsers).Load();
        return course.ToSettingsViewModel(userId, courseRole.Value, courseRole == CourseUserRole.Admin);
    }

	internal async Task<IEnumerable<Dto.User>> GetCourseUsers(long courseId, long userId)
	{
		return await _context.CourseUsers
			.Where(cu => cu.CourseId == courseId)
			.Where(cu => cu.UserId != userId)
			.Include(cu => cu.User)
			.Select(cu => cu.User.ToViewModel())
			.ToListAsync();
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

	// Update
	internal async Task<bool> CanManageCourse(long courseId, long userId)
	{
		var courseRole = (await _context.CourseUsers.FindAsync(courseId, userId))?.Role;
		return courseRole == CourseUserRole.Admin;
	}
	public async Task<bool> UpdateCourse(Dto.UpdateCourseRequest request, long userId)
	{
		var course = _context.Courses.Find(request.CourseId);
		if (course == null) return false;

		var updatingUser = _context.CourseUsers.Find(request.CourseId, userId);

		_context.Entry(course).Collection(c => c.CourseUsers).Load();
		
		List<CourseUser> newCourseUsers = request.Members.Select(member => new CourseUser
		{
			CourseId = course.Id,
			UserId = member.UserId,
			Role = member.Role.ToEntity()
		}).ToList();

		if (updatingUser is not null)
		{
			newCourseUsers.Add(updatingUser);
		}

		var deletedUsersIds = course.CourseUsers.Select(cu => cu.UserId).Except(newCourseUsers.Select(ncu => ncu.UserId)).ToHashSet();
		var deletedUserFolderPermissionsQuery = _context.UserFolderPermissions.Where(ufp => ufp.Folder.CourseId == course.Id && deletedUsersIds.Contains(ufp.UserId));
		_context.RemoveRange(deletedUserFolderPermissionsQuery);
		
		course.Name = request.Name;
		course.Description = request.Description;
		course.CourseUsers = newCourseUsers;

		await _context.SaveChangesAsync();
		return true;
	}
	
	// Delete
	internal async Task<bool> CanDeleteCourse(long courseId, long userId)
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
