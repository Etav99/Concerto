using Concerto.Shared.Models.Dto;
using Nito.AsyncEx;

namespace Concerto.Client.Services;

public interface ICourseService
{
	public IEnumerable<Dto.Course> Courses { get; }
	public Dictionary<long, IEnumerable<Dto.Session>> CourseSessions { get; }
	public Task LoadCoursesAsync();
	public Task LoadCourseSessions(long courseId);

    public Task<bool> CreateCourse(CreateCourseRequest request);

    public void InvalidateCache();
}
public class CourseService : ICourseService
{
	private readonly ICourseClient _courseClient;
	private readonly ISessionClient _sessionClient;

	private List<Dto.Course> _coursesCache = new();
	private Dictionary<long, IEnumerable<Dto.Session>> _courseSessionsCache = new();
	private bool _cacheInvalidated = true;
	private readonly AsyncLock _mutex = new AsyncLock();
	public CourseService(ICourseClient courseClient, ISessionClient sessionClient)
	{
		_courseClient = courseClient;
		_sessionClient = sessionClient;
	}

	public IEnumerable<Dto.Course> Courses => _coursesCache;
	public Dictionary<long, IEnumerable<Dto.Session>> CourseSessions => _courseSessionsCache;

	public void InvalidateCache()
	{
		_cacheInvalidated = true;
		_courseSessionsCache.Clear();
	}

	public async Task LoadCoursesAsync()
	{
		using (await _mutex.LockAsync())
		{
			if (!_cacheInvalidated) return;
			var response = await _courseClient.GetCurrentUserCoursesAsync();
			_coursesCache = response?.ToList() ?? new List<Dto.Course>();
			_cacheInvalidated = false;
		}
	}
	public async Task LoadCourseSessions(long courseId)
	{
		using (await _mutex.LockAsync())
		{
			if (_courseSessionsCache.ContainsKey(courseId)) return;
			var response = await _sessionClient.GetCourseSessionsAsync(courseId);
			_courseSessionsCache.Add(courseId, response?.ToList() ?? new List<Dto.Session>());
		}
	}

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
}
