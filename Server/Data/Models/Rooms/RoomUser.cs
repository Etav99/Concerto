namespace Concerto.Server.Data.Models;

public class RoomUser
{
	public long RoomId { get; set; }
	public Room Room { get; set; }
	public long UserId { get; set; }
	public User User { get; set; }
	public RoomUserRole Role { get; set; }
}

public enum RoomUserRole
{
	Owner = 0,
	Admin = 1,
	Member = 2,
}