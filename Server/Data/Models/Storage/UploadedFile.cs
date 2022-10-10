using System.ComponentModel.DataAnnotations;

namespace Concerto.Server.Data.Models;

public class UploadedFile
{
    [Key]
    public long Id { get; set; }

    public long CatalogId { get; set; }
    public Catalog Catalog { get; set; }

    public string DisplayName { get; set; }
    public string StorageName { get; set; }
    public string Path {
        get
        {
            return $"/var/lib/concerto/storage/{CatalogId}/{StorageName}";
        }
    }

}
