namespace Concerto.Shared.Models.Dto;

public record UploadedFile(long Id, string Name) : EntityModel(Id);

public record FileUploadResult
{
	public bool Uploaded { get; set; } = false;
	public string? DisplayFileName { get; set; }
	public string? Extension { get; set; }
	public int ErrorCode { get; set; } = 0;
	public string ErrorMessage { get; set; } = string.Empty;
}

public record FileSettings(long Id, string Name);

public record UpdateFileRequest
{
	public long FileId { get; set; }
	public string Name { get; set; } = null!;
}

public record MoveFileRequest
{
	public long FileId { get; set; }
	public long MoveIntoFolderId { get; set; }
}

public record CopyFileRequest
{
	public long FileId { get; set; }
	public long CopyIntoFolderId { get; set; }
}


