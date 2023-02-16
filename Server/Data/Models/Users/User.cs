﻿using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Concerto.Shared.Extensions;
using Microsoft.EntityFrameworkCore;

namespace Concerto.Server.Data.Models;

[Index(nameof(SubjectId), IsUnique = true)]
public class User : Entity
{
	public User() { }

	public User(ClaimsPrincipal claimsPrincipal)
	{
		SubjectId = claimsPrincipal.GetSubjectId();
		Username = claimsPrincipal.GetUsername();
		FirstName = claimsPrincipal.GetFirstName();
		LastName = claimsPrincipal.GetLastName();
	}

	public Guid SubjectId { get; set; }

	[Required] public string Username { get; set; } = null!;

	public string FirstName { get; set; } = null!;
	public string LastName { get; set; } = null!;

	public string FullName => $"{FirstName} {LastName}";
}

public static partial class ViewModelConversions
{
	public static Dto.User ToViewModel(this User user)
	{
		return new Dto.User(user.Id, user.Username, user.FirstName, user.LastName);
	}

	public static IEnumerable<Dto.User> ToViewModel(this IEnumerable<User>? users)
	{
		if (users == null)
			return Enumerable.Empty<Dto.User>();
		return users.Select(c => c.ToViewModel());
	}
}
