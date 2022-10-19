using Concerto.Shared.Extensions;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace Concerto.Server.Data.Models;

[Index(nameof(SubjectId), IsUnique = true)]
public class User : Entity
{
	public Guid SubjectId { get; set; }
	[Required]
	public string Username { get; set; }
	public string FirstName { get; set; }
	public string LastName { get; set; }
	public virtual ICollection<Contact>? InvitedContacts { get; set; }
	public virtual ICollection<Contact>? InvitingContacts { get; set; }
	public virtual ICollection<ConversationUser>? ConversationsUser { get; set; }
	public virtual ICollection<RoomUser>? RoomsUser { get; set; }
	public virtual ICollection<Catalog> CatalogsSharedTo { get; set; } = null!;

	public User() { }

	public User(ClaimsPrincipal claimsPrincipal)
	{
		SubjectId = claimsPrincipal.GetSubjectId();
		Username = claimsPrincipal.GetUsername();
		FirstName = claimsPrincipal.GetFirstName();
		LastName = claimsPrincipal.GetLastName();
	}
}
