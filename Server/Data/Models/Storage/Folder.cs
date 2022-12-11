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
	Video,
	Audio,
	Documents,
	Other
}

public static partial class ViewModelConversions
{
    public static Dto.FolderItem ToFolderItem(this Folder folder, bool canWrite, bool canEdit, bool canDelete)
    {
		return new Dto.FolderItem(
			Id: folder.Id,
			Name: folder.Name,
			CanWrite: canWrite,
			CanEdit: canEdit,
			CanDelete: canDelete,
			Type: folder.Type.ToViewModel()
		);
    }

    public static Dto.FolderType ToViewModel(this FolderType type)
	{
        return type switch
        {
            FolderType.CourseRoot => Dto.FolderType.CourseRoot,
            FolderType.Sheets => Dto.FolderType.Sheets,
            FolderType.Recordings => Dto.FolderType.Recordings,
            FolderType.Video => Dto.FolderType.Video,
            FolderType.Audio => Dto.FolderType.Audio,
            FolderType.Documents => Dto.FolderType.Documents,
            FolderType.Other => Dto.FolderType.Other,
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
        };
    }
	public static FolderType ToEntity(this Dto.FolderType type)
	{
        return type switch
        {
            Dto.FolderType.CourseRoot => FolderType.CourseRoot,
            Dto.FolderType.Sheets => FolderType.Sheets,
            Dto.FolderType.Recordings => FolderType.Recordings,
            Dto.FolderType.Video => FolderType.Video,
            Dto.FolderType.Audio => FolderType.Audio,
            Dto.FolderType.Documents => FolderType.Documents,
            Dto.FolderType.Other => FolderType.Other,
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
        };
    }

}
