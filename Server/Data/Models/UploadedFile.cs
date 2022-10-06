using System.ComponentModel.DataAnnotations;

namespace Concerto.Server.Data.Models;

public class UploadedFile
{
    [Key]
    public long Id { get; set; }

    public long SessionId { get; set; }
    public Session Session { get; set; }

    public string DisplayName { get; set; }
    public string StorageName { get; set; }
    public string Path {
        get
        {
            return $"UserFileStorage/Sessions/{SessionId}/{StorageName}";
        }
    }
}
