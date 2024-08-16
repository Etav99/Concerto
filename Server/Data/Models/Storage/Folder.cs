using Concerto.Shared.Models.Dto;
using Microsoft.EntityFrameworkCore;

namespace Concerto.Server.Data.Models;

[Index(nameof(ParentId))]
public class Folder : Entity
{
	public string Name { get; set; } = null!;
	public FolderType Type { get; set; } = FolderType.Other;

	public long WorkspaceId { get; set; }
	public Workspace Workspace { get; set; } = null!;

	public Guid? OwnerId { get; set; }
	public User? Owner { get; set; }

	public long? ParentId { get; set; }
	public Folder? Parent { get; set; }

	public virtual ICollection<Folder> SubFolders { get; set; } = null!;
	public virtual ICollection<UploadedFile> Files { get; set; } = null!;

	public FolderPermission WorkspacePermission { get; set; } = null!;
	public virtual ICollection<UserFolderPermission> UserPermissions { get; set; } = null!;

	public bool IsWorkspaceRoot => Type is FolderType.WorkspaceRoot;
	public bool IsPermanent => Type is FolderType.WorkspaceRoot or FolderType.Sessions;

	public static Folder NewRoot(long workspaceId)
	{
		// Create workspace root folder, with default read permissions for workspace members
		var rootFolder = new Folder
		{
			WorkspacePermission = new FolderPermission { Inherited = false, Type = FolderPermissionType.Read },
			WorkspaceId = workspaceId,
			Name = "Root",
			Type = FolderType.WorkspaceRoot,
		};

		return rootFolder;
	}

	public static Folder NewSessionsFolder(long workspaceId)
	{
		// Create workspace sessions folder, with default read permissions for workspace members
		var sessionsFolder = new Folder
		{
			WorkspacePermission = new FolderPermission { Inherited = false, Type = FolderPermissionType.Read },
			WorkspaceId = workspaceId,
			Name = "Sessions",
			Type = FolderType.Sessions
		};
		return sessionsFolder;
	}
}

public enum FolderType
{
	WorkspaceRoot,
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
		return new FolderItem(
			folder.Id,
			folder.Name,
			folder.Owner?.FullName ?? "-",
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
			FolderType.WorkspaceRoot => Dto.FolderType.WorkspaceRoot,
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
			Dto.FolderType.WorkspaceRoot => FolderType.WorkspaceRoot,
			Dto.FolderType.Sessions => FolderType.Sessions,
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

