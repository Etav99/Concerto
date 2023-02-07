using System.Security.Claims;
using Concerto.Server.Data.DatabaseContext;
using Concerto.Server.Data.Models;
using Concerto.Shared.Extensions;
using Microsoft.EntityFrameworkCore;

namespace Concerto.Server.Services;

public class UserService
{
	private readonly AppDataContext _context;
	private readonly ILogger<UserService> _logger;

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
		var user = await _context.Users.FindAsync(userId);
		return user?.ToViewModel();
	}

	public async Task<Dto.User?> GetUser(Guid subjectId)
	{
		var user = await _context.Users.Where(u => u.SubjectId == subjectId).FirstOrDefaultAsync();
		return user?.ToViewModel();
	}

	public async Task<long> GetUserIdAndUpdate(ClaimsPrincipal userClaimsPrincipal)
	{
		var user = await _context.Users
			.Where(u => u.SubjectId == userClaimsPrincipal.GetSubjectId())
			.FirstOrDefaultAsync();

		if (user is not null)
		{
			if (user.Username != userClaimsPrincipal.GetUsername()
			    || user.FirstName != userClaimsPrincipal.GetFirstName()
			    || user.LastName != userClaimsPrincipal.GetLastName())
			{
				user.Username = userClaimsPrincipal.GetUsername();
				user.LastName = userClaimsPrincipal.GetLastName();
				user.FirstName = userClaimsPrincipal.GetFirstName();
				await _context.SaveChangesAsync();
			}
		}
		else
		{
			user = new Data.Models.User(userClaimsPrincipal);
			await _context.Users.AddAsync(user);
			await _context.SaveChangesAsync();
		}

		return user.Id;
	}

	public async Task<IEnumerable<Dto.User>> GetUsers(long userId)
	{
		var users = await _context.Users
			.Where(u => u.Id != userId)
			.Select(u => u.ToViewModel())
			.ToListAsync();
		return users;
	}

	public async Task<IEnumerable<Dto.User>> SearchWithoutUser(long userId, string searchString)
	{
		return await _context.Users
			.Where(u => u.Id != userId && (EF.Functions.ILike(u.Username, $"%{searchString}%")
			                               || EF.Functions.ILike(u.FirstName + " " + u.LastName, $"%{searchString}%"))
			)
			.Select(u => u.ToViewModel())
			.ToListAsync();
	}

	public async Task<IEnumerable<Dto.User>> Search(string searchString)
	{
		return await _context.Users
			.Where(u => EF.Functions.ILike(u.Username, $"%{searchString}%")
			            || EF.Functions.ILike(u.FirstName + " " + u.LastName, $"%{searchString}%")
			)
			.Select(u => u.ToViewModel())
			.ToListAsync();
	}
}

