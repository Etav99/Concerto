namespace Concerto.Server.Data.Models;

public class UploadedFile : Entity
{
	public long CatalogId { get; set; }
	public Catalog Catalog { get; set; }

	public string DisplayName { get; set; }
	public string StorageName { get; set; }
	public string Path
	{
		get
		{
			return $"/var/lib/concerto/storage/{CatalogId}/{StorageName}";
		}
	}

}
