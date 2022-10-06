using Concerto.Server.Data.DatabaseContext;
using Concerto.Server.Data.Models;
using Concerto.Server.Extensions;
using Concerto.Server.Data.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Concerto.Server.Services;

public class SessionService
{
    private readonly ILogger<SessionService> _logger;
    private readonly AppDataContext _context;
    private readonly FileService _fileService;
    public SessionService(ILogger<SessionService> logger, AppDataContext context, FileService fileService)
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
            .Reference(s => s.Conversation)
            .Query()
            .Include(c => c.ConversationUsers)
            .ThenInclude(cu => cu.User)
            .LoadAsync();

        return session.ToDto();
    }

    public async Task<bool> IsUserSessionMember(long userId, long sessionId)
    {
        var session = await _context.Sessions
            .FindAsync(sessionId);

        if (session == null)
            return false;

        return await _context.Entry(session)
            .Reference(s => s.Room)
            .Query()
            .Include(r => r.RoomUsers)
            .AnyAsync(r => r.RoomUsers.Any(ru => ru.UserId == userId));
    }

    public async Task<bool> CreateSession(Dto.CreateSessionRequest request)
    {
        var room = await _context.Rooms
            .Include(r => r.RoomUsers)
            .ThenInclude(ru => ru.User)
            .FirstOrDefaultAsync(r => r.RoomId == request.RoomId);

        if (room == null)
            return false;

        var session = new Session();
        var sessionConversation = room.RoomUsers.Select(ru => ru.User).ToGroupConversation();
        session.Name = request.Name;
        session.ScheduledDate = request.ScheduledDateTime.ToUniversalTime();
        session.Conversation = sessionConversation;
        session.Room = room;

        await _context.Sessions.AddAsync(session);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<IEnumerable<Dto.FileUploadResult>> UploadSessionFiles(IEnumerable<IFormFile> files, long sessionId)
    {
        var fileUploadResults = await _fileService.UploadFiles(files);
        
        foreach(var fileUploadResult in fileUploadResults){
            if (fileUploadResult.Uploaded && !String.IsNullOrEmpty(fileUploadResult.DisplayFileName) && !String.IsNullOrEmpty(fileUploadResult.StorageFileName))
            {
                var uploadedFile = new UploadedFile()
                {
                    DisplayName = fileUploadResult.DisplayFileName,
                    StorageName = fileUploadResult.StorageFileName,
                    SessionId = sessionId
                };
                 _context.Add(uploadedFile);
            }
        }
        
        _context.SaveChanges();
        return fileUploadResults.ToDto();
    }
}
