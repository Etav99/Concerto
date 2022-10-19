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

	public async Task<Dto.Room?> GetRoom(long roomId)
	{
		var room = await _context.Rooms.FindAsync(roomId);
		if (room == null)
			return null;

		await _context.Entry(room)
			.Collection(r => r.RoomUsers)
			.Query()
			.Include(ru => ru.User)
			.LoadAsync();
		await _context.Entry(room)
			.Reference(r => r.Conversation)
			.Query()
			.Include(c => c.ConversationUsers)
			.ThenInclude(cu => cu.User)
			.LoadAsync();
		await _context.Entry(room)
			.Collection(r => r.Sessions)
			.Query()
			.Include(s => s.Conversation)
			.ThenInclude(c => c.ConversationUsers)
			.LoadAsync();
		return room.ToDto();
	}
	public async Task<IEnumerable<Dto.Room>> GetUserRooms(long userId)
	{
		return await _context.Rooms
			.Include(r => r.RoomUsers)
			.Where(r => r.RoomUsers.Any(ru => ru.UserId == userId))
			.Include(r => r.RoomUsers)
			.ThenInclude(ru => ru.User)
			.Include(r => r.Conversation)
			.ThenInclude(c => c.ConversationUsers)
			.ThenInclude(cu => cu.User)
			.Select(r => r.ToDto())
			.ToListAsync();
	}

	public async Task<bool> IsUserRoomMember(long userId, long roomId)
	{
		var room = await _context.Rooms
			.FindAsync(roomId);

		if (room == null)
			return false;

		return await _context.Entry(room)
			.Collection(c => c.RoomUsers)
			.Query()
			.AnyAsync(cu => cu.UserId == userId);
	}

	public async Task<bool> CreateRoom(Dto.CreateRoomRequest request, long userId)
	{
		var userIds = request.Members.Select(m => m.Id).Append(userId).Distinct();
		var users = await _context.Users
			.Where(u => userIds.Contains(u.Id))
			.ToListAsync();

		if (users.Count != request.Members.Count() + 1)
			return false;

		var room = new Room();
		var roomConversation = users.ToGroupConversation();
		var roomUsers = users.Select(u => new RoomUser
		{
			User = u,
			Room = room,
		}).ToList();
		room.OwnerId = userId;
		room.Name = request.Name;
		room.Conversation = roomConversation;
		room.RoomUsers = roomUsers;

		var deg = await _context.Rooms.AddAsync(room);
		await _context.SaveChangesAsync();
		return true;
	}
}
