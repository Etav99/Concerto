using Nito.AsyncEx;
using System.Net.Http.Json;

namespace Concerto.Client.Services;
public interface IStorageService
{
    public IEnumerable<Dto.Catalog> OwnedCatalogs { get; }
    public IEnumerable<Dto.Catalog> SharedCatalogs { get; }
    public IEnumerable<Dto.Catalog> SessionCatalogs(long sessionId);

    public Task LoadOwnedCatalogsAsync();
    public Task LoadSharedCatalogsAsync();
    public Task LoadSessionCatalogsAsync(long sessionId);
    public Task LoadCatalogFilesAsync(Dto.Catalog Catalog);
    public void InvalidateCache();
}

public class StorageService : IStorageService
{
    private readonly IStorageClient _storageClient;

    public StorageService(IStorageClient storageClient)
    {
        _storageClient = storageClient;
    }

    private readonly AsyncLock _mutex = new AsyncLock();

    private bool _ownedCatalogsCacheInvalidated = true;
    private List<Dto.Catalog> _ownedCatalogsCache = new();

    private bool _sharedCatalogsCacheInvalidated = true;
    private List<Dto.Catalog> _sharedCatalogsCache = new();

    private Dictionary<long, List<Dto.Catalog>> _sessionCatalogs = new();

    public IEnumerable<Dto.Catalog> OwnedCatalogs => _ownedCatalogsCache;
    public IEnumerable<Dto.Catalog> SharedCatalogs => _sharedCatalogsCache;

    public IEnumerable<Dto.Catalog> SessionCatalogs(long sessionId)
    {
        return _sessionCatalogs.ContainsKey(sessionId) ? _sessionCatalogs[sessionId]
                                                       : Enumerable.Empty<Dto.Catalog>();
    }

    public async Task LoadOwnedCatalogsAsync()
    {
        using(await _mutex.LockAsync())
        {
            if (!_ownedCatalogsCacheInvalidated) return;
            var response = await _storageClient.GetCurrentUserCatalogsAsync();
            _ownedCatalogsCache = response?.ToList() ?? new List<Dto.Catalog>();
            _ownedCatalogsCacheInvalidated = false;
        }
    }

    public async Task LoadSharedCatalogsAsync()
    {
        using (await _mutex.LockAsync())
        {
            if (!_sharedCatalogsCacheInvalidated) return;
            var response = await _storageClient.GetCatalogsSharedForCurrentUserAsync();
            _sharedCatalogsCache = response?.ToList() ?? new List<Dto.Catalog>();
            _sharedCatalogsCacheInvalidated = false;
        }
    }

    public async Task LoadSessionCatalogsAsync(long sessionId)
    {
        using (await _mutex.LockAsync())
        {
            if (_sessionCatalogs.ContainsKey(sessionId)) return;
            var response = await _storageClient.GetSessionCatalogsAsync(sessionId);
            _sessionCatalogs.Add(sessionId, response?.ToList() ?? new List<Dto.Catalog>());
        }
    }

    public async Task LoadCatalogFilesAsync(Dto.Catalog catalog)
    {
        var response = await _storageClient.GetCatalogFilesAsync(catalog.Id);
        catalog.Files = response?.ToList() ?? new List<Dto.UploadedFile>();
    }

    public void InvalidateCache()
    {
        _ownedCatalogsCacheInvalidated = true;
        _sharedCatalogsCacheInvalidated = true;
        _sessionCatalogs.Clear();
    }
}
