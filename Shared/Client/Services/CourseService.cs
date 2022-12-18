using Concerto.Shared.Models.Dto;

namespace Concerto.Shared.Client.Services;

public interface ICourseService : ICourseClient
{
	public EventHandler<IEnumerable<CourseListItem>>? UserCoursesFetchEventHandler { get; set; }
}

public class CourseService : CourseClient, ICourseService
{
	public CourseService(HttpClient httpClient) : base(httpClient) { }

	public EventHandler<IEnumerable<CourseListItem>>? UserCoursesFetchEventHandler { get; set; }

	public override async Task<ICollection<CourseListItem>> GetCurrentUserCoursesAsync()
	{
		var courses = await base.GetCurrentUserCoursesAsync();
		UserCoursesFetchEventHandler?.Invoke(this, courses);
		return courses;
	}
}

public interface ISessionService : ISessionClient { }

public class SessionService : SessionClient, ISessionService
{
	public SessionService(HttpClient httpClient) : base(httpClient) { }
}


