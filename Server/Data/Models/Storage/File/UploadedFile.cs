using Concerto.Server.Settings;

namespace Concerto.Server.Data.Models;

public class UploadedFile : Entity
{
	public long FolderId { get; set; }
	public Folder Folder { get; set; }

    public long OwnerId { get; set; }

    public string DisplayName { get; set; }
	public string StorageName { get; set; }
	public string Path
	{
		get
		{
			return $"{AppSettings.Storage.StoragePath}/{FolderId}/{StorageName}";
		}
	}

}

public static partial class ViewModelConversions
{
    public static Dto.FileItem ToFileItem(this UploadedFile file, bool canManage)
    {
		return new Dto.FileItem(
			Id: file.Id,
			Name: file.DisplayName,
			CanEdit: canManage,
			CanDelete: canManage
		);
    }
}
