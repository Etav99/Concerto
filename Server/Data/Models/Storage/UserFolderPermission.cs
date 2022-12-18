namespace Concerto.Server.Data.Models;

public class UserFolderPermission
{
	public long UserId { get; set; }
	public User User { get; set; } = null!;

	public long FolderId { get; set; }
	public Folder Folder { get; set; } = null!;

	public FolderPermission Permission { get; set; } = null!;

	public static object ToKey(long UserId, long FolderId)
	{
		return new { UserId, FolderId };
	}
}

public static partial class ViewModelConversions
{
	public static Dto.UserFolderPermission ToViewModel(this UserFolderPermission userFolderPermission)
	{
		return new Dto.UserFolderPermission(Permission: userFolderPermission.Permission.ToViewModel(),
			User: userFolderPermission.User.ToViewModel()
		);
	}

	public static UserFolderPermission ToEntity(this Dto.UserFolderPermission userFolderPermission, bool? inherited = null)
	{
		return new UserFolderPermission
		{
			UserId = userFolderPermission.User.Id,
			Permission = userFolderPermission.Permission.ToEntity(inherited ?? userFolderPermission.Permission.Inherited)
		};
	}
}
