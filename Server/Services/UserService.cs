using Concerto.Server.Data.DatabaseContext;
using Concerto.Server.Data.Models;
using Concerto.Shared.Extensions;
using Concerto.Shared.Models.Dto;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Concerto.Server.Services;

public class UserService
{
	private readonly AppDataContext _context;
	private readonly IdentityManagerService _identityManagerService;
	private readonly ILogger<UserService> _logger;

	public UserService(ILogger<UserService> logger, AppDataContext context, IdentityManagerService identityManagerService)
	{
		_logger = logger;
		_context = context;
		_identityManagerService = identityManagerService;
	}

	public async Task<Dto.User?> GetUser(Guid userId)
	{
		var user = await _context.Users.FindAsync(userId);
		return user?.ToViewModel();
	}

	public async Task<bool> UpdateUser(ClaimsPrincipal userClaimsPrincipal)
	{
		var user = await _context.Users.FindAsync(userClaimsPrincipal.GetSubjectId());

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
			return true;
		}
		return false;
	}

	public async Task<IEnumerable<Dto.User>> GetUsers(Guid userId)
	{
		var users = await _context.Users
			.Where(u => u.Id != userId)
			.Select(u => u.ToViewModel())
			.ToListAsync();
		return users;
	}

	public async Task<IEnumerable<Dto.User>> SearchWithoutUser(Guid userId, string searchString)
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

	public async Task<IEnumerable<Dto.UserIdentity>> GetUnverifiedUserIdentities()
	{
		return await _identityManagerService.GetUnverifiedUsers();
	}

	public async Task<IEnumerable<Dto.UserIdentity>> GetUserIdentities(Guid userId)
	{
		var user = await _context.Users.FindAsync(userId);
		if (user is null)
			return Enumerable.Empty<Dto.UserIdentity>();
		return await _identityManagerService.GetUsers(user.Id);
	}

	public async Task VerifyUser(Guid subjectId)
	{
		await _identityManagerService.VerifyUser(subjectId);
		var user = await _identityManagerService.UserFromIdentity(subjectId);
		await _context.Users.AddAsync(user);
		await _context.SaveChangesAsync();
	}

	public async Task DeleteUser(Guid subjectId)
	{
		await _identityManagerService.DeleteUser(subjectId);
		var user = await _context.Users.FindAsync(subjectId);
		if (user is not null)
		{
			_context.Users.Remove(user);
			await _context.SaveChangesAsync();
		}
	}

	public async Task SetUserRole(Guid subjectId, Role role)
	{
		await _identityManagerService.SetRole(subjectId, role);
	}
}

