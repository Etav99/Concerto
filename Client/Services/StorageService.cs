using Nito.AsyncEx;

namespace Concerto.Client.Services;
public interface IStorageService
{
    public IEnumerable<Dto.CatalogListItem> OwnedCatalogs { get; }
    public IEnumerable<Dto.CatalogListItem> SharedCatalogs { get; }
    public IEnumerable<Dto.CatalogListItem> SessionCatalogs(long sessionId);

    public Task LoadOwnedCatalogsAsync();
    public Task LoadSharedCatalogsAsync();
    public Task LoadSessionCatalogsAsync(long sessionId);
    public Task<Dto.CatalogContent> GetCatalogContent(long catalogId);
    public Task<Dto.CatalogSettings> GetCatalogSettings(long catalogId);


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
    private List<Dto.CatalogListItem> _ownedCatalogsCache = new();

    private bool _sharedCatalogsCacheInvalidated = true;
    private List<Dto.CatalogListItem> _sharedCatalogsCache = new();

    private Dictionary<long, List<Dto.CatalogListItem>> _sessionCatalogs = new();

    public IEnumerable<Dto.CatalogListItem> OwnedCatalogs => _ownedCatalogsCache;
    public IEnumerable<Dto.CatalogListItem> SharedCatalogs => _sharedCatalogsCache;

    public IEnumerable<Dto.CatalogListItem> SessionCatalogs(long sessionId)
    {
        return _sessionCatalogs.ContainsKey(sessionId) ? _sessionCatalogs[sessionId]
                                                       : Enumerable.Empty<Dto.CatalogListItem>();
    }

    public async Task LoadOwnedCatalogsAsync()
    {
        using (await _mutex.LockAsync())
        {
            if (!_ownedCatalogsCacheInvalidated) return;
            var response = await _storageClient.GetOwnedCatalogsAsync();
            _ownedCatalogsCache = response?.ToList() ?? new List<Dto.CatalogListItem>();
            _ownedCatalogsCacheInvalidated = false;
        }
    }

    public async Task LoadSharedCatalogsAsync()
    {
        using (await _mutex.LockAsync())
        {
            if (!_sharedCatalogsCacheInvalidated) return;
            var response = await _storageClient.GetSharedCatalogsAsync();
            _sharedCatalogsCache = response?.ToList() ?? new List<Dto.CatalogListItem>();
            _sharedCatalogsCacheInvalidated = false;
        }
    }

    public async Task LoadSessionCatalogsAsync(long sessionId)
    {
        using (await _mutex.LockAsync())
        {
            if (_sessionCatalogs.ContainsKey(sessionId)) return;
            var response = await _storageClient.GetSessionCatalogsAsync(sessionId);
            _sessionCatalogs.Add(sessionId, response?.ToList() ?? new List<Dto.CatalogListItem>());
        }
    }

    public async Task<Dto.CatalogContent> GetCatalogContent(long catalogId) => await _storageClient.GetCatalogContentAsync(catalogId);
    public async Task<Dto.CatalogSettings> GetCatalogSettings(long catalogId) => await _storageClient.GetCatalogSettingsAsync(catalogId);

    public void InvalidateCache()
    {
        _ownedCatalogsCacheInvalidated = true;
        _sharedCatalogsCacheInvalidated = true;
        _sessionCatalogs.Clear();
    }
}
