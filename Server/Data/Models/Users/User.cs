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
    public string Username { get; set; } = null!;
    public string FirstName { get; set; } = null!;
    public string LastName { get; set; } = null!;
    public virtual ICollection<Contact> InvitedContacts { get; set; } = null!;
	public virtual ICollection<Contact> InvitingContacts { get; set; } = null!;
	public virtual ICollection<ConversationUser> ConversationsUser { get; set; } = null!;
	public virtual ICollection<CourseUser> CoursesUser { get; set; } = null!;

	public string FullName => $"{FirstName} {LastName}";

	public User() { }

	public User(ClaimsPrincipal claimsPrincipal)
	{
		SubjectId = claimsPrincipal.GetSubjectId();
		Username = claimsPrincipal.GetUsername();
		FirstName = claimsPrincipal.GetFirstName();
		LastName = claimsPrincipal.GetLastName();
	}
}
