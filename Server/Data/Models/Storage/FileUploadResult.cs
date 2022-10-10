namespace Concerto.Server.Data.Models;

public record FileUploadResult
{
    public bool Uploaded { get; set; } = false;
    public string? DisplayFileName { get; set; }
    public string? StorageFileName { get; set; }
    public int ErrorCode { get; set; } = 0;
}
