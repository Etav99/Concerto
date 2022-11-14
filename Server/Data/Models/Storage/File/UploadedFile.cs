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
