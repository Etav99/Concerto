using Concerto.Server.Extensions;

namespace Concerto.Server.Data.Models;

public class Folder : Entity
{
    public string Name { get; set; } = null!;
    public FolderType Type { get; set; } = FolderType.Other;

    public long CourseId { get; set; }
    public Course Course { get; set; } = null!;

    public long OwnerId { get; set; }
    public User Owner { get; set; } = null!;

    public long? ParentId { get; set; }
    public Folder? Parent { get; set; }

    public virtual ICollection<Folder> SubFolders { get; set; } = null!;
    public virtual ICollection<UploadedFile> Files { get; set; } = null!;

    public FolderPermission CoursePermission { get; set; }
    public virtual ICollection<UserFolderPermission> UserPermissions { get; set; } = null!;

    public bool IsCourseRoot  => Type == FolderType.CourseRoot;
}
public enum FolderType
{
    CourseRoot,
    Sheets,
    Recordings,
    Other,
}

public static partial class ViewModelConversions
{
	public static Dto.FolderSettings ToFolderSettings(this Folder folder)
	{
		return new Dto.FolderSettings
		{
			Id = folder.Id,
			Name = folder.Name,
			OwnerId = folder.OwnerId,
			CoursePermission = folder.CoursePermission.ToDto(),
			UserPermissions = folder.UserPermissions.Select(fp => fp.ToDto()).ToList(),
		};
	}
	
	//public static Dto.FolderItem ToFolderListItem(this Folder folder, bool canWrite, bool canEdit, bool canDelete)
	//{
	//    return new Dto.FolderItem
	//    {
	//        Id = folder.Id,
	//        Name = folder.Name,
	//        CanWrite = canWrite,
	//        CanEdit = canEdit,
	//        CanDelete = canDelete,
	//    };
	//}

	//public static Dto.FolderContent ToFolderContent(this Folder folder)
	//{
	//    return new Dto.FolderContent
	//    {
	//        Id = folder.Id,
	//        Self = folder.ToFolderListItem(),
	//        SubFolders = folder.SubFolders.Select(f => f.ToFolderListItem()),
	//        Files = folder.Files.ToDto(),
	//    };
	//}
}
