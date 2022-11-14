namespace Concerto.Shared.Models.Dto;

public record FolderContent : EntityModel
{
	public FolderItem Self { get; init; } = null!;
	public virtual IEnumerable<FolderItem> SubFolders { get; init; } = Enumerable.Empty<FolderItem>();
	public virtual IEnumerable<FileItem> Files { get; init; } = Enumerable.Empty<FileItem>();
}


public record FolderContentItem : EntityModel
{
    public string Name { get; init; } = null!;
}

public record FolderItem : FolderContentItem
{
	public bool CanWrite { get; init; }
	public bool CanEdit { get; init; }
	public bool CanDelete { get; init; }
}

public record FileItem : FolderContentItem
{
	public bool CanEdit { get; init; }
	public bool CanDelete { get; init; }
}

public record FolderSettings : EntityModel
{
	public string Name { get; init; } = string.Empty;
	public long OwnerId { get; init; }
    public FolderPermission CoursePermission { get; init; } = null!;
    public virtual IEnumerable<UserFolderPermission> UserPermissions { get; init; } = null!;
}

public record UserFolderPermission
{
    public User User { get; set; } = null!;
    public FolderPermission Permission { get; set; } = null!;
}

public record FolderPermission
{
    public FolderPermissionType Type { get; set; }
    public bool Inherited { get; set; }
}

public enum FolderPermissionType
{
    Read = 0,
    ReadWriteOwned = 1,
    ReadWrite = 2,
}

public record CreateFolderRequest
{
	public string Name { get; set; } = null!;
    public long ParentId { get; set; }
    public FolderPermission CoursePermission { get; init; } = null!;
    public virtual IEnumerable<UserFolderPermission> UserPermissions { get; init; } = null!;
}

public record UpdateFolderRequest : EntityModel
{
	public string Name { get; set; }
    public FolderPermission CoursePermission { get; init; } = null!;
    public virtual IEnumerable<UserFolderPermission> UserPermissions { get; init; } = null!;
    public bool PropagatePermissions { get; set; }
}