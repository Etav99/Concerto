﻿using Concerto.Client.Services;
using Concerto.Server.Data.DatabaseContext;
using Concerto.Server.Data.Models;
using Concerto.Server.Settings;
using Microsoft.EntityFrameworkCore;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.Json;

namespace Concerto.Server.Services;

public class SessionService
{
	private readonly AppDataContext _context;
	private readonly StorageService _storageService;
	private readonly ILogger<SessionService> _logger;

	public SessionService(ILogger<SessionService> logger, AppDataContext context, StorageService storageService)
	{
		_logger = logger;
		_context = context;
		_storageService = storageService;
	}

	public async Task<(long courseId, long sessionId)> GetCourseAndSessionIds(Guid meetingGuid)
	{
		var session = await _context.Sessions.Where(s => s.MeetingGuid == meetingGuid).SingleAsync();
		return (session.CourseId, session.Id);
	}

	public async Task<Dto.Session?> GetSession(long sessionId, Guid userId, bool isAdmin)
	{
		var session = await _context.Sessions
			.FindAsync(sessionId);

		if (session == null)
			return null;

		await _context.Entry(session)
			.Reference(s => s.Course)
			.LoadAsync();

		return session.ToViewModel(isAdmin || await CanManageSession(sessionId, userId));
	}

	public async Task<bool> CanAccessSession(Guid meetingGuid, Guid userId)
	{
		var session = await _context.Sessions.Where(s => s.MeetingGuid == meetingGuid).FirstOrDefaultAsync();
		if (session == null)
			return false;

		var courseUser = await _context.CourseUsers.FindAsync(session.CourseId, userId);
		if (courseUser == null)
			return false;

		return true;
	}

	public async Task<bool> CanAccessSession(long sessionId, Guid userId)
	{
		var session = await _context.Sessions.FindAsync(sessionId);
		if (session == null)
			return false;

		var courseUser = await _context.CourseUsers.FindAsync(session.CourseId, userId);
		if (courseUser == null)
			return false;

		return true;
	}

	internal async Task<bool> CanManageSession(long sessionId, Guid userId)
	{
		var session = await _context.Sessions.FindAsync(sessionId);
		if (session == null) return false;

		var courseRole = (await _context.CourseUsers.FindAsync(session.CourseId, userId))?.Role;
		return courseRole is CourseUserRole.Admin or CourseUserRole.Supervisor;
	}

	internal async Task<bool> DeleteSession(long sessionId)
	{
		var session = await _context.Sessions.FindAsync(sessionId);
		if (session == null) return false;

		// change folder type to recordings
		var folder = await _context.Folders.FindAsync(session.FolderId);
		if (folder != null)
		{
			folder.Type = FolderType.Recordings;
		}
		_context.Remove(session);
		await _context.SaveChangesAsync();

		if(folder != null && !(await _context.Folders.AnyAsync(f => f.ParentId == folder.Id) || await _context.UploadedFiles.AnyAsync(f => f.FolderId == folder.Id)))
		{
			_context.Folders.Remove(folder);
			await _context.SaveChangesAsync();
		}

		return true;
	}

	public async Task<long?> CreateSession(Dto.CreateSessionRequest request, Guid ownerId)
	{
		var course = await _context.Courses
			.Include(r => r.CourseUsers)
			.ThenInclude(ru => ru.User)
			.FirstOrDefaultAsync(r => r.Id == request.CourseId);

		if (course == null || !course.SessionsFolderId.HasValue)
			return null;

		var createFolderRequest = new Dto.CreateFolderRequest
		{
			ParentId = course.SessionsFolderId.Value!,
			Name = request.Name,
			Type = Dto.FolderType.Sessions,
			CoursePermission = new Dto.FolderPermission(Dto.FolderPermissionType.ReadWriteOwned, false)
		};

		var folderId = await _storageService.CreateFolder(createFolderRequest, ownerId);
		if (folderId == null)
			return null;

		var session = new Data.Models.Session
		{
			Name = request.Name,
			ScheduledDate = request.ScheduledDateTime.ToUniversalTime(),
			Course = course,
			FolderId = folderId.Value
		};

		await _context.Sessions.AddAsync(session);
		await _context.SaveChangesAsync();
		return session.Id;
	}

	internal async Task<IEnumerable<Dto.SessionListItem>> GetCourseSessions(long courseId)
	{
		return await _context.Sessions
			.Where(s => s.Course.Id == courseId)
			.Select(s => s.ToSessionListItem())
			.ToListAsync();
	}

	internal async Task<bool> UpdateSession(Dto.UpdateSessionRequest request)
	{
		var session = await _context.Sessions.FindAsync(request.SessionId);
		if (session == null)
			return false;

		session.Name = request.Name;
		session.ScheduledDate = request.ScheduledDateTime.ToUniversalTime();

		await _context.Entry(session).Reference(s => s.Folder).LoadAsync();
		session.Folder.Name = request.Name;
		await _context.SaveChangesAsync();
		return true;
	}

	public async Task<Dto.SessionSettings?> GetSessionSettings(long sessionId)
	{
		var session = await _context.Sessions.FindAsync(sessionId);

		return session?.ToSettingsViewModel();
	}

	public async Task<string> GenerateMeetingToken(Guid userId, long sessionId)
	{
		var session = await _context.Sessions.FindAsync(sessionId);
		if (session == null)
			throw new Exception("Session not found");
		return await GenerateMeetingToken(userId, session.MeetingGuid);
	}

	public async Task<string> GenerateMeetingToken(Guid userId, Guid roomGuid)
	{
		var user = await _context.Users.FindAsync(userId);
		if (user == null)
			throw new Exception("User not found");

		string key = AppSettings.Meetings.JwtSecret;
		var securityKey = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
		var credentials = new Microsoft.IdentityModel.Tokens.SigningCredentials(securityKey, "HS256");

		var jitsiContext = new { 
			user = new
			{
				avatar = "", 
				email = "", 
				name = user.FullName
			}
		};

        ClaimsIdentity claimsIdentity = new ClaimsIdentity();
        claimsIdentity.AddClaim(new Claim("context", JsonSerializer.Serialize(jitsiContext), JsonClaimValueTypes.Json));
        claimsIdentity.AddClaim(new Claim("room", roomGuid.ToString()));
        claimsIdentity.AddClaim(new Claim("sub", (new Uri(AppSettings.Meetings.JitsiUrl)).Host));

		var tokenHandler = new JwtSecurityTokenHandler();
		var token = tokenHandler.CreateJwtSecurityToken(
			issuer: AppSettings.Meetings.JwtAppId,
			audience: (new Uri(AppSettings.Meetings.JitsiUrl)).Host,
			subject: claimsIdentity,
			expires: DateTime.UtcNow.AddMinutes(2),
			signingCredentials: credentials
		);
		Console.WriteLine(token.ToString());
		return tokenHandler.WriteToken(token);
	}
}

