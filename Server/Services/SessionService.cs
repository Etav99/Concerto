using Concerto.Server.Data.DatabaseContext;
using Concerto.Server.Data.Models;
using Concerto.Server.Extensions;
using Microsoft.EntityFrameworkCore;

namespace Concerto.Server.Services;

public class SessionService
{
	private readonly ILogger<SessionService> _logger;
	private readonly AppDataContext _context;
	private readonly StorageService _fileService;
	public SessionService(ILogger<SessionService> logger, AppDataContext context, StorageService fileService)
	{
		_logger = logger;
		_context = context;
		_fileService = fileService;
	}
	public async Task<Dto.Session?> GetSession(long sessionId)
	{
		var session = await _context.Sessions
			.FindAsync(sessionId);

		if (session == null)
			return null;

        await _context.Entry(session)
            .Reference(s => s.Course)
            .LoadAsync();

        await _context.Entry(session)
			.Reference(s => s.Conversation)
			.Query()
			.Include(c => c.ConversationUsers)
			.ThenInclude(cu => cu.User)
			.LoadAsync();

		return session.ToDto();
	}

	public async Task<bool> CanAccessSession(long userId, long sessionId)
	{
		var session = await _context.Sessions.FindAsync(sessionId);
		if (session == null)
			return false;

        var courseUser = await _context.CourseUsers.FindAsync(new { session.CourseId, userId });

        return await _context.Entry(session)
			.Reference(s => s.Course)
			.Query()
			.Include(r => r.CourseUsers)
			.AnyAsync(r => r.CourseUsers.Any(ru => ru.UserId == userId));
	}

	public async Task<bool> CreateSession(Dto.CreateSessionRequest request)
	{
		var course = await _context.Courses
			.Include(r => r.CourseUsers)
			.ThenInclude(ru => ru.User)
			.FirstOrDefaultAsync(r => r.Id == request.CourseId);

		if (course == null)
			return false;

		var session = new Session();
		var sessionConversation = course.CourseUsers.Select(ru => ru.User).ToGroupConversation();
		session.Name = request.Name;
		session.ScheduledDate = request.ScheduledDateTime.ToUniversalTime();
		session.Conversation = sessionConversation;
		session.Course = course;

		await _context.Sessions.AddAsync(session);
		await _context.SaveChangesAsync();
		return true;
	}

	internal async Task<IEnumerable<Dto.Session>> GetCourseSessions(long courseId)
	{
		return await _context.Sessions
			.Where(s => s.Course.Id == courseId)
			.Select(s => s.ToDto())
			.ToListAsync();
	}
}
