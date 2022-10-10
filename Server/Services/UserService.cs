using Concerto.Server.Data.DatabaseContext;
using Concerto.Server.Data.Models;
using Concerto.Server.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

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

    public async Task<Dto.User?> GetUser(long userId)
    {
        User? user = await _context.Users.FindAsync(userId);
        if (user == null) return null;
        return user.ToDto();
    }

    public async Task<Dto.User?> GetUser(Guid subjectId)
    {
        return await _context.Users.Where(u => u.SubjectId == subjectId).Select(u => u.ToDto()).FirstOrDefaultAsync();
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
            .Where(u => u.Id != userId && (u.Username.Contains(searchString) || (u.FirstName + " " + u.LastName).Contains(searchString)))
            .Select(u => u.ToDto())
            .ToListAsync();
    }

    public async Task<IEnumerable<Dto.User>> Search(string searchString)
    {
        return await _context.Users
            .Where(u => u.Username.Contains(searchString) || (u.FirstName + " " + u.LastName).Contains(searchString))
            .Select(u => u.ToDto())
            .ToListAsync();
    }

}
