namespace Concerto.Server.Data.Models;

public class Contact
{
	public User User1 { get; set; }
	public long User1Id { get; set; }
	public User User2 { get; set; }
	public long User2Id { get; set; }
	public ContactStatus Status { get; set; }
}

public enum ContactStatus
{
	Pending = 0,
	Accepted = 1,
	Rejected = 2,
}