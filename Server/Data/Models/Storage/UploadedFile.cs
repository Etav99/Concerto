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
	public string DisplayFileName { get; set; }  = string.Empty;
	public string Extension { get; set; } = string.Empty;
    public string StorageFileName { get; set; } = string.Empty;
    public int ErrorCode { get; set; } = 0;
	public string ErrorMessage { get; set; } = string.Empty;
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

	public static IEnumerable<Dto.FileUploadResult> ToViewModel(this IEnumerable<FileUploadResult> fileUploadResults)
	{
		return fileUploadResults.Select(u => u.ToViewModel());
	}

	public static Dto.FileUploadResult ToViewModel(this FileUploadResult fileUploadResult)
	{
		return new Dto.FileUploadResult
		{
			DisplayFileName = fileUploadResult.DisplayFileName,
			ErrorCode = fileUploadResult.ErrorCode,
			Uploaded = fileUploadResult.Uploaded,
			ErrorMessage = fileUploadResult.ErrorMessage,
		};
	}

	public static Dto.UploadedFile ToViewModel(this UploadedFile file)
	{
		return new Dto.UploadedFile(
			Id: file.Id,
			Name: file.DisplayName
		);
	}
	public static IEnumerable<Dto.UploadedFile> ToViewModel(this IEnumerable<UploadedFile> files)
	{
		return files.Select(u => u.ToViewModel());
	}
}
