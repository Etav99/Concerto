using Concerto.Server.Data.DatabaseContext;
using Concerto.Server.Data.Models;
using Concerto.Shared.Models.Dto;
using Microsoft.EntityFrameworkCore;
using Course = Concerto.Shared.Models.Dto.Course;
using CourseUser = Concerto.Server.Data.Models.CourseUser;
using CourseUserRole = Concerto.Server.Data.Models.CourseUserRole;
using FolderPermission = Concerto.Server.Data.Models.FolderPermission;
using FolderPermissionType = Concerto.Server.Data.Models.FolderPermissionType;
using FolderType = Concerto.Server.Data.Models.FolderType;
using User = Concerto.Shared.Models.Dto.User;

namespace Concerto.Server.Services;

public class CourseService
{
	private readonly AppDataContext _context;
	private readonly ILogger<CourseService> _logger;
	private readonly StorageService _storageService;

	public CourseService(ILogger<CourseService> logger, AppDataContext context, StorageService storageService)
	{
		_logger = logger;
		_context = context;
		_storageService = storageService;
	}

	// Create
	public async Task<long> CreateCourse(CreateCourseRequest request, long userId)
	{
		var membersRoles = request.Members.ToDictionary(m => m.UserId, m => m.Role.ToEntity());
		membersRoles.Add(userId, CourseUserRole.Admin);

		var members = await _context.Users
			.Where(u => membersRoles.Keys.Contains(u.Id))
			.ToListAsync();

		if (members.Count != membersRoles.Count)
			return 0;

		// Create course
		var course = new Data.Models.Course
		{
			OwnerId = userId,
			Name = request.Name,
			CreatedDate = DateTime.UtcNow
		};

		// Add members to course
		var courseUsers = members.Select(member => new CourseUser
				{
					CourseId = course.Id, UserId = member.Id, Role = membersRoles[member.Id]
				}
			)
			.ToList();
		course.CourseUsers = courseUsers;

		await _context.Courses.AddAsync(course);
		await _context.SaveChangesAsync();

		// Create course root folder, with default read permissions for course members
		var rootFolder = new Folder
		{
			CoursePermission = new FolderPermission { Inherited = false, Type = FolderPermissionType.Read },
			OwnerId = userId,
			CourseId = course.Id,
			Name = course.Name,
			Type = FolderType.CourseRoot
		};
		await _context.Folders.AddAsync(rootFolder);
		await _context.SaveChangesAsync();

		course.RootFolderId = rootFolder.Id;
		await _context.SaveChangesAsync();

		return course.Id;
	}

	// Read
	public async Task<bool> IsUserCourseMember(long userId, long courseId)
	{
		var courseUser = await _context.CourseUsers.FindAsync(courseId, userId);
		return courseUser != null;
	}

	public async Task<Course?> GetCourse(long courseId, long userId, bool isAdmin = false)
	{
		var course = await _context.Courses.FindAsync(courseId);
		return course?.ToViewModel(isAdmin || await CanManageCourse(courseId, userId));
	}

	public async Task<CourseSettings?> GetCourseSettings(long courseId, long userId, bool isAdmin = false)
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

		await _context.Entry(course).Collection(c => c.CourseUsers).LoadAsync();
		return course.ToSettingsViewModel(userId, courseRole.Value, courseRole == CourseUserRole.Admin);
	}

	internal async Task<IEnumerable<User>> GetCourseUsers(long courseId)
	{
		return await _context.CourseUsers
			.Where(cu => cu.CourseId == courseId)
			.Include(cu => cu.User)
			.Select(cu => cu.User.ToViewModel())
			.ToListAsync();
	}

	public async Task<IEnumerable<CourseListItem>> GetUserCoursesList(long userId)
	{
		return await _context.CourseUsers
			.Where(cu => cu.UserId == userId)
			.Include(cu => cu.Course)
			.Select(cu => cu.Course.ToCourseListItem())
			.ToListAsync();
	}

	public async Task<IEnumerable<CourseListItem>> GetAllCourses()
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

	public async Task<bool> UpdateCourse(UpdateCourseRequest request, long userId)
	{
		var course = await _context.Courses.FindAsync(request.CourseId);
		if (course == null) return false;

		var updatingUser = await _context.CourseUsers.FindAsync(request.CourseId, userId);

		await _context.Entry(course).Collection(c => c.CourseUsers).LoadAsync();

		var newCourseUsers = request.Members.Select(member => new CourseUser
				{
					CourseId = course.Id, UserId = member.UserId, Role = member.Role.ToEntity()
				}
			)
			.ToList();

		if (updatingUser is not null) newCourseUsers.Add(updatingUser);

		var deletedUsersIds = course.CourseUsers.Select(cu => cu.UserId).Except(newCourseUsers.Select(ncu => ncu.UserId)).ToHashSet();
		var deletedUserFolderPermissionsQuery
			= _context.UserFolderPermissions.Where(ufp => ufp.Folder.CourseId == course.Id && deletedUsersIds.Contains(ufp.UserId));
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

	internal async Task<bool> DeleteCourse(long courseId)
	{
		var course = await _context.Courses.FindAsync(courseId);
		if (course == null) return false;

		_context.Remove(course);
		await _context.SaveChangesAsync();
		return true;
	}

	internal async Task<long> CloneCourse(CloneCourseRequest request, long userId)
	{
		var course = await _context.Courses.FindAsync(request.CourseId);
		if (course == null)
			return 0;

		var createCourseRequest = new CreateCourseRequest
		{
			Name = request.Name, Description = request.Description, Members = Enumerable.Empty<Dto.CourseUser>()
		};

		var courseId = await CreateCourse(createCourseRequest, userId);
		if (courseId == 0)
			return 0;

		var newCourse = await _context.Courses.FindAsync(courseId);
		if (newCourse == null)
			return 0;

		await _context.Entry(newCourse).Collection(c => c.CourseUsers).LoadAsync();


		if (request.CopyCourseUsers)
		{
			var copiedCourseUsers = await _context.CourseUsers
				.AsNoTracking()
				.Where(cu => cu.CourseId == request.CourseId)
				.ToListAsync();

			copiedCourseUsers.ForEach(cu => cu.CourseId = newCourse.Id);

			if (!request.CopyRoles)
				copiedCourseUsers.ForEach(cu => cu.Role = cu.UserId == userId ? CourseUserRole.Admin : CourseUserRole.Member);
			newCourse.CourseUsers = copiedCourseUsers;
		}


		if (!request.CopyCourseUsers) request.CopyFoldersPermissions = false;
		if (request.CopyFolders && course.RootFolderId is not null)
		{
			var rootFolderCopy = await _storageService.CreateFolderCopy(course.RootFolderId.Value, request.CourseId, request.CopyFiles,
				request.CopyFoldersPermissions
			);
			if (rootFolderCopy is not null)
			{
				await _context.Entry(newCourse).Reference(c => c.RootFolder).LoadAsync();
				if (newCourse.RootFolder is not null) _context.Remove(newCourse.RootFolder);

				rootFolderCopy.ParentId = null;
				newCourse.RootFolder = rootFolderCopy;
				newCourse.RootFolder.Name = newCourse.Name;
			}
		}

		await _context.SaveChangesAsync();
		return newCourse.Id;
	}
}

