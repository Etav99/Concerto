using Concerto.Server.Settings;
using Concerto.Shared.Models.Dto;
using Microsoft.EntityFrameworkCore;

namespace Concerto.Server.Data.Models;

[Index(nameof(FolderId))]
public class UploadedFile : Entity
{
	public long FolderId { get; set; }
	public Folder Folder { get; set; } = null!;

	public Guid? OwnerId { get; set; }

	public long Size { get; set; }

	public string DisplayName { get; set; } = string.Empty;
	public string Extension { get; set; } = string.Empty;
	public string MimeType { get; set; } = string.Empty;
	public string StorageName { get; set; } = string.Empty;
	public string Path => $"{AppSettings.Storage.StoragePath}/{StorageName}";
}

public record FileUploadResult
{
	public bool Uploaded { get; set; } = false;
	public string DisplayFileName { get; set; } = string.Empty;
	public string Extension { get; set; } = string.Empty;
	public string StorageFileName { get; set; } = string.Empty;
	public string StorageDir { get; set; } = string.Empty;
	public int ErrorCode { get; set; } = 0;
	public string ErrorMessage { get; set; } = string.Empty;
}

public class FilenameException : Exception
{
	public FilenameException(string message) : base(message) { }
}

public static partial class ViewModelConversions
{
	public static FileItem ToFileItem(this UploadedFile file, bool canManage)
	{
		return new FileItem(file.Id,
			file.DisplayName,
			file.Extension,
			file.MimeType,
			file.Size,
			canManage,
			canManage
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
			ErrorMessage = fileUploadResult.ErrorMessage
		};
	}
}

