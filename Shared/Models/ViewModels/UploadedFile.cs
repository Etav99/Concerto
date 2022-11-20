namespace Concerto.Shared.Models.Dto;

public record UploadedFile(long Id, string Name) : EntityModel(Id);

public record FileUploadResult
{
	public bool Uploaded { get; set; } = false;
	public string? DisplayFileName { get; set; }
	public int ErrorCode { get; set; } = 0;
}
