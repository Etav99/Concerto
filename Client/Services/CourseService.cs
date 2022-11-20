using Concerto.Client.Pages;
using Concerto.Shared.Models.Dto;
using Nito.AsyncEx;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Concerto.Client.Services;

public interface ICourseService
{
	public Task<IEnumerable<Dto.CourseListItem>> GetUserCoursesList();
	public Task<Dto.Course> GetCourse(long courseId);
	public Task<bool> CreateCourse(CreateCourseRequest request);
	public Task<IEnumerable<SessionListItem>> GetCourseSessionsList(long courseId);
}
public class CourseService : ICourseService
{
	private readonly ICourseClient _courseClient;
	private readonly ISessionClient _sessionClient;

	public CourseService(ICourseClient courseClient, ISessionClient sessionClient)
	{
		_courseClient = courseClient;
		_sessionClient = sessionClient;
	}

	public async Task<Course> GetCourse(long courseId) => await _courseClient.GetCourseAsync(courseId);
	public async Task<IEnumerable<CourseListItem>> GetUserCoursesList() => await _courseClient.GetCurrentUserCoursesAsync();

	public async Task<bool> CreateCourse(CreateCourseRequest request)
	{
		try
		{
			await _courseClient.CreateCourseForCurrentUserAsync(request);
			return true;
		}
		catch (CourseException e)
		{
			Console.WriteLine(e);
			return false;
		}
	}

	public async Task<IEnumerable<SessionListItem>> GetCourseSessionsList(long courseId) => await _sessionClient.GetCourseSessionsAsync(courseId);
}