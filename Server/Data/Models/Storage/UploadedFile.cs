using Concerto.Server.Settings;

namespace Concerto.Server.Data.Models;

public class UploadedFile : Entity
{
	public long FolderId { get; set; }
	public Folder Folder { get; set; } = null!;

    public long OwnerId { get; set; }

	public string DisplayName { get; set; } = string.Empty;

	public string Extension { get; set; } = string.Empty;
	public string StorageName { get; set; } = string.Empty;
	public string Path
	{
		get
		{
			return $"{AppSettings.Storage.StoragePath}/{FolderId}/{StorageName}";
		}
	}

}

public record FileUploadResult
{
	public bool Uploaded { get; set; } = false;
	public string? DisplayFileName { get; set; }
	public string? Extension { get; set; }
	public string? StorageFileName { get; set; }
	public int ErrorCode { get; set; } = 0;
}

public class FilenameException : Exception
{
	public FilenameException(string message) : base(message) {}
}

public static partial class ViewModelConversions
{
    public static Dto.FileItem ToFileItem(this UploadedFile file, bool canManage)
    {
		return new Dto.FileItem(
			Id: file.Id,
			Name: file.DisplayName,
			Extension: file.Extension,
			CanEdit: canManage,
			CanDelete: canManage
		);
    }
}
