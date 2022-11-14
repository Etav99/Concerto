using Concerto.Server.Extensions;
using Concerto.Shared.Models.Dto;

namespace Concerto.Server.Data.Models;
public class UserFolderPermission
{
    public long UserId { get; set; }
    public User User { get; set; } = null!;
    
    public long FolderId { get; set; }
    public Folder Folder { get; set; } = null!;

    public FolderPermission Permission { get; set; }

    public static Object ToKey(long UserId, long FolderId)
    {
        return new { UserId, FolderId };
    }
}

public static partial class ViewModelConversions
{
    public static Dto.UserFolderPermission ToDto(this UserFolderPermission userFolderPermission)
    {
        return new Dto.UserFolderPermission
        {
            Permission = userFolderPermission.Permission.ToDto(),
            User = userFolderPermission.User.ToDto()
        };
    }
    
    public static UserFolderPermission ToEntity(this Dto.UserFolderPermission userFolderPermission)
    {
        return new UserFolderPermission
        {
            UserId = userFolderPermission.User.Id,
            Permission = userFolderPermission.Permission.ToEntity()
        };
    }
    
}