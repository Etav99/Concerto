using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Concerto.Shared.Models.Dto;

public record FolderContent
{
	public FolderPermission CoursePermission { get; init; } = null!;
	public FolderItem Self { get; init; } = null!;
	public virtual IEnumerable<FolderItem> SubFolders { get; init; } = Enumerable.Empty<FolderItem>();
	public virtual IEnumerable<FileItem> Files { get; init; } = Enumerable.Empty<FileItem>();
}


public record FolderContentItem(
	long Id,
	string Name
) : EntityModel(Id);

public record FolderItem(
	long Id,
	string Name, 
	bool CanWrite, 
	bool CanEdit,
	bool CanDelete, 
	FolderType Type
) : FolderContentItem(Id, Name);

public record FileItem(
	long Id,
	string Name,
	bool CanEdit,
	bool CanDelete
) : FolderContentItem(Id, Name);

public record FolderSettings(
	long Id,
	string Name,
	long OwnerId,
	long CourseId,
	FolderType Type,
	FolderPermission CoursePermission,
	FolderPermission? ParentCoursePermission,
    IEnumerable<UserFolderPermission> UserPermissions,
    IEnumerable<UserFolderPermission> ParentUserPermissions
) : EntityModel(Id);

public record UserFolderPermission(User User, FolderPermission Permission)
{
	public User User { get; set; } = User;
	public FolderPermission Permission { get; set; } = Permission;
}

public record FolderPermission(FolderPermissionType Type, bool Inherited);

public enum FolderPermissionType
{
    Read = 0,
    ReadWriteOwned = 1,
    ReadWrite = 2,
}

public enum FolderType
{
	CourseRoot,
	Sheets,
	Recordings,
	Other,
}

public record CreateFolderRequest
{
	public string Name { get; set; } = string.Empty;
    public long ParentId { get; set; }
    public FolderPermission CoursePermission { get; set; } = null!;
}

public record UpdateFolderRequest : EntityModel
{
	public string Name { get; set; } = null!;
	public FolderPermission CoursePermission { get; set; } = null!;
    public virtual HashSet<UserFolderPermission> UserPermissions { get; set; } = null!;
    public bool forceInherit { get; set; }

	public UpdateFolderRequest(long Id) : base(Id) { }
}