using Concerto.Shared.Models.Dto;
using Nito.AsyncEx;

namespace Concerto.Client.Services;
public interface IStorageService
{
    public Task<Dto.FolderContent> GetFolderContent(long folderId);
    public Task<Dto.FolderSettings> GetFolderSettings(long folderId);
    public Task DeleteFolder(long folderId);
    public Task DeleteFile(long fileId);
    public Task CreateFolder(CreateFolderRequest request);
}

public class StorageService : IStorageService
{
    private readonly IStorageClient _storageClient;

    public StorageService(IStorageClient storageClient)
    {
        _storageClient = storageClient;
    }

    public async Task CreateFolder(CreateFolderRequest request) => await _storageClient.CreateFolderAsync(request);
    public async Task DeleteFolder(long folderId) => await _storageClient.DeleteFolderAsync(folderId);
    public async Task DeleteFile(long fileId) => await _storageClient.DeleteFileAsync(fileId);
    public async Task<Dto.FolderContent> GetFolderContent(long folderId) => await _storageClient.GetFolderContentAsync(folderId);
    public async Task<Dto.FolderSettings> GetFolderSettings(long folderId) => await _storageClient.GetFolderSettingsAsync(folderId);
}
