using Concerto.Server.Data.DatabaseContext;
using Concerto.Server.Data.Models;
using Concerto.Server.Extensions;
using Concerto.Shared.Extensions;
using Microsoft.EntityFrameworkCore;
using System.Runtime.CompilerServices;
using System.Security.Claims;
using static Microsoft.Extensions.Logging.EventSource.LoggingEventSource;

namespace Concerto.Server.Services;

public class UserService
{
	private readonly ILogger<UserService> _logger;
    
	private readonly AppDataContext _context;
	public UserService(ILogger<UserService> logger, AppDataContext context)
	{
		_logger = logger;
		_context = context;
	}

    public async Task<long?> GetUserId(Guid subjectId)
	{
        var user = await _context.Users.Where(u => u.SubjectId == subjectId).FirstOrDefaultAsync();
		return user?.Id;
    }
    
	public async Task<Dto.User?> GetUser(long userId)
	{
		User? user = await _context.Users.FindAsync(userId);
		if (user == null) return null;
		return user.ToDto();
	}

	public async Task<Dto.User?> GetUser(Guid subjectId)
	{
        User? user = await _context.Users.Where(u => u.SubjectId == subjectId).FirstOrDefaultAsync();
        if (user == null) return null;
        return user.ToDto();
    }

	public async Task<bool> AddUserIfNotExists(ClaimsPrincipal userClaimsPrincipal)
	{
		User? user = await _context.Users
			.Where(u => u.SubjectId == userClaimsPrincipal.GetSubjectId())
			.FirstOrDefaultAsync();

		if (user == null)
		{
			user = new User(userClaimsPrincipal);
			await _context.Users.AddAsync(user);
			await _context.SaveChangesAsync();

			// Add all users to contacts
			var allUsers = await _context.Users.ToListAsync();
            List<Contact> contacts = allUsers.Select(u => new Contact { User1Id = user.Id, User2Id = u.Id }).ToList();


			// Create conversations with all users
			List<Conversation> conversations = new();
            foreach (var u in allUsers)
            {
                if (u.Id != user.Id)
                {
                    var conversation = new Conversation { IsPrivate = true };
                    var conversationUsers = new List<ConversationUser>
                    {
                        new() { Conversation = conversation, User = user },
                        new() { Conversation = conversation, User = u }
                    };
                    conversation.ConversationUsers = conversationUsers;
					conversations.Add(conversation);
                }
            }

            await _context.Contacts.AddRangeAsync(contacts);
            await _context.Conversations.AddRangeAsync(conversations);
            await _context.SaveChangesAsync();
            return true;
		}
		return false;
    }

	public async Task<IEnumerable<Dto.User>> GetUserContacts(long userId)
	{
		IEnumerable<Dto.User>? contacts = await _context.Contacts
			.Where(c => c.User1Id == userId || c.User2Id == userId)
			.Include(c => c.User1)
			.Include(c => c.User2)
			.Select(c => c.User1Id == userId ? c.User2 : c.User1)
			.Select(c => c.ToDto())
			.ToListAsync();
		contacts ??= Enumerable.Empty<Dto.User>();
		return contacts;
	}

	public async Task<IEnumerable<Dto.User>> SearchWithoutUser(long userId, string searchString)
	{
		return await _context.Users
			.Where(u => u.Id != userId && (EF.Functions.ILike(u.Username, $"%{searchString}%") || EF.Functions.ILike(u.FirstName + " " + u.LastName, $"%{searchString}%")))
            .Select(u => u.ToDto())
			.ToListAsync();
	}

	public async Task<IEnumerable<Dto.User>> Search(string searchString)
	{
		return await _context.Users
			.Where(u => EF.Functions.ILike(u.Username, $"%{searchString}%") || EF.Functions.ILike(u.FirstName + " " + u.LastName, $"%{searchString}%"))
			.Select(u => u.ToDto())
			.ToListAsync();
	}

}
