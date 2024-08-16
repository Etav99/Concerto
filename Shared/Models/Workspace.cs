using System.Diagnostics.CodeAnalysis;

namespace Concerto.Shared.Models.Dto;

public record Workspace(
	long Id,
	string Name,
	string Description,
	long RootFolderId,
	bool CanManage
) : EntityModel(Id);

public record WorkspaceSettings(
	long Id,
	string Name,
	string Description,
	IEnumerable<WorkspaceUser> Members,
	WorkspaceUserRole CurrentUserRole,
	bool CanManage
) : EntityModel(Id);

public record WorkspaceListItem(long Id, string Name, string Description, DateTime CreatedDate) : EntityModel(Id);

public record WorkspaceUser(Guid UserId, WorkspaceUserRole Role)
{
	public WorkspaceUserRole Role { get; set; } = Role;
}

public class WorkspaceUserIdEqualityComparer : IEqualityComparer<WorkspaceUser>
{
	public bool Equals(WorkspaceUser? x, WorkspaceUser? y)
	{
		return x?.UserId == y?.UserId;
	}

	public int GetHashCode([DisallowNull] WorkspaceUser obj)
	{
		return obj.UserId.GetHashCode();
	}
}

public enum WorkspaceUserRole
{
	Admin = 0,
	Supervisor = 1,
	Member = 2
}

public static class WorkspaceUserRoleExtensions
{
	public static string ToDisplayString(this WorkspaceUserRole role)
	{
		return role switch
		{
			WorkspaceUserRole.Admin => "Administrator",
			WorkspaceUserRole.Supervisor => "Supervisor",
			WorkspaceUserRole.Member => "Member",
			_ => throw new ArgumentOutOfRangeException(nameof(role), role, null)
		};
	}
}

public record CreateWorkspaceRequest
{
	public string Name { get; set; } = null!;
	public string Description { get; set; } = string.Empty;
}

public record UpdateWorkspaceRequest
{
	public long WorkspaceId { get; set; }
	public string Name { get; set; } = null!;
	public string Description { get; set; } = string.Empty;
	public HashSet<WorkspaceUser> Members { get; set; } = null!;
}

public record CloneWorkspaceRequest
{
	public long WorkspaceId { get; set; }
	public string Name { get; set; } = string.Empty;
	public string Description { get; set; } = string.Empty;

	public bool CopyWorkspaceUsers { get; set; } = true;
	public bool CopyRoles { get; set; } = true;

	public bool CopyFolders { get; set; } = true;
	public bool CopyFoldersPermissions { get; set; } = true;
	public bool CopyFiles { get; set; } = false;
}


