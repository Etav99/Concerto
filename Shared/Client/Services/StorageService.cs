namespace Concerto.Shared.Client.Services;

public interface IStorageService : IStorageClient { }

public class StorageService : StorageClient, IStorageService
{
	public StorageService(HttpClient httpClient) : base(httpClient) { }
}

