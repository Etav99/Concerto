using Concerto.Server.Data.DatabaseContext;
using Concerto.Server.Data.Models;
using Concerto.Server.Extensions;
using Microsoft.EntityFrameworkCore;

namespace Concerto.Server.Services;

public class RoomService
{
    private readonly ILogger<RoomService> _logger;

    private readonly AppDataContext _context;
    public RoomService(ILogger<RoomService> logger, AppDataContext context)
    {
        _logger = logger;
        _context = context;
    }

    public async Task<Room?> GetRoom(long roomId)
    {
        return await _context.Rooms.FindAsync(roomId);
    }
    public async Task<IEnumerable<Dto.Room>> GetUserRooms(long userId)
    {
        return await _context.Rooms
            .Include(r => r.RoomUsers)
            .Where(r => r.RoomUsers.Any(ru => ru.UserId == userId))
            .Include(r => r.RoomUsers)
            .ThenInclude(ru => ru.User)
            //.Include(r => r.Sessions)
            .Include(r => r.Conversation)
            .ThenInclude(c => c.ConversationUsers)
            .ThenInclude(cu => cu.User)
            .Select(r => r.ToDto())
            .ToListAsync();
    }

}
