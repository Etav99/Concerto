using Concerto.Client.Pages;
using Concerto.Shared.Models.Dto;
using Microsoft.EntityFrameworkCore;

namespace Concerto.Server.Data.Models;

[Index(nameof(ParentId))]
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

	public FolderPermission CoursePermission { get; set; } = null!;
	public virtual ICollection<UserFolderPermission> UserPermissions { get; set; } = null!;
	
	public bool IsCourseRoot => Type is FolderType.CourseRoot;
	public bool IsPermanent => Type is FolderType.CourseRoot or FolderType.Sessions;

	public static Folder NewRoot(long courseId, long ownerId)
	{
		// Create course sessions folder, with default read permissions for course members
		var sessionsFolder = new Folder
		{
			CoursePermission = new FolderPermission { Inherited = false, Type = FolderPermissionType.Read },
			OwnerId = ownerId,
			CourseId = courseId,
			Name = "Sessions",
			Type = FolderType.Sessions
		};

		// Create course root folder, with default read permissions for course members
		var rootFolder = new Folder
		{
			CoursePermission = new FolderPermission { Inherited = false, Type = FolderPermissionType.Read },
			OwnerId = ownerId,
			CourseId = courseId,
			Name = "Root",
			Type = FolderType.CourseRoot,
			SubFolders = new List<Folder>{sessionsFolder}
		};

		return rootFolder;
	}
}

public enum FolderType
{
	CourseRoot,
	Sessions,
	Sheets,
	Recordings,
	Video,
	Audio,
	Documents,
	Other
}

public static partial class ViewModelConversions
{
	public static FolderItem ToFolderItem(this Folder folder, bool canWrite, bool canEdit, bool canDelete)
	{
		return new FolderItem(folder.Id,
			folder.Name,
			canWrite,
			canEdit,
			canDelete,
			folder.Type.ToViewModel()
		);
	}

	public static Dto.FolderType ToViewModel(this FolderType type)
	{
		return type switch
		{
			FolderType.CourseRoot => Dto.FolderType.CourseRoot,
			FolderType.Sessions => Dto.FolderType.Sessions,
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

