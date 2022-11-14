using Concerto.Server.Extensions;
using Concerto.Shared.Models.Dto;
using Microsoft.EntityFrameworkCore;

namespace Concerto.Server.Data.Models;

[Owned]
public class FolderPermission
{
    public FolderPermissionType Type { get; set; }
    public bool Inherited { get; set; }
}

public enum FolderPermissionType
{
    Read = 0,
    ReadWriteOwned = 1,
    ReadWrite = 2,
    Max = ReadWrite
}

public static partial class ViewModelConversions
{
    public static Dto.FolderPermission ToDto(this FolderPermission permission)
    {
        return new Dto.FolderPermission
        {
            Type = permission.Type.ToDto(),
            Inherited = permission.Inherited,
        };
    }
    
    public static FolderPermission ToEntity(this Dto.FolderPermission permission)
    {
        return new FolderPermission
        {
            Type = permission.Type.ToEntity(),
            Inherited = false,
        };
    }
    public static Dto.FolderPermissionType ToDto(this FolderPermissionType permissionType)
    {
        switch (permissionType)
        {
            case FolderPermissionType.Read:
                return Dto.FolderPermissionType.Read;
            case FolderPermissionType.ReadWrite:
                return Dto.FolderPermissionType.ReadWrite;
            case FolderPermissionType.ReadWriteOwned:
                return Dto.FolderPermissionType.ReadWriteOwned;
            default:
                throw new ArgumentOutOfRangeException(nameof(permissionType), permissionType, null);
        }
    }
    public static FolderPermissionType ToEntity(this Dto.FolderPermissionType permissionType)
    {
        switch (permissionType)
        {
            case Dto.FolderPermissionType.Read:
                return FolderPermissionType.Read;
            case Dto.FolderPermissionType.ReadWrite:
                return FolderPermissionType.ReadWrite;
            case Dto.FolderPermissionType.ReadWriteOwned:
                return FolderPermissionType.ReadWriteOwned;
            default:
                throw new ArgumentOutOfRangeException(nameof(permissionType), permissionType, null);
        }
    }
}