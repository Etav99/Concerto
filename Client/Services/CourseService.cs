using Concerto.Client.Pages;
using Concerto.Shared.Models.Dto;
using MudBlazor;
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
	public Task<CourseSettings> GetCourseSettings(long courseId);
	public Task<IEnumerable<User>> GetCourseUsers(long courseId);
	public Task UpdateCourse(UpdateCourseRequest request);
}
public class CourseService : ICourseService
{
	private readonly ICourseClient _courseClient;
	private readonly ISessionClient _sessionClient;
	private readonly ISnackbar _snackbar;

	public CourseService(ICourseClient courseClient, ISessionClient sessionClient, ISnackbar snackbar)
	{
		_courseClient = courseClient;
		_sessionClient = sessionClient;
		_snackbar = snackbar;
	}

	public async Task<Course> GetCourse(long courseId) => await _courseClient.GetCourseAsync(courseId);
	public async Task<CourseSettings> GetCourseSettings(long courseId) => await _courseClient.GetCourseSettingsAsync(courseId);
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

	public async Task UpdateCourse(UpdateCourseRequest request)
	{
		try
		{
			await _courseClient.UpdateCourseAsync(request);
		}
		catch
		{
			_snackbar.Add("Failed to update course", Severity.Error);
		}
	}

    public async Task<IEnumerable<User>> GetCourseUsers(long courseId) => await _courseClient.GetCourseUsersAsync(courseId);
}