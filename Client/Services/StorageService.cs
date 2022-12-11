using Concerto.Shared.Models.Dto;
using Nito.AsyncEx;

namespace Concerto.Client.Services;
public interface IStorageService
{
    public Task<Dto.FolderContent> GetFolderContent(long folderId);
    public Task<Dto.FolderSettings> GetFolderSettings(long folderId);
    public Task DeleteFolder(long folderId);
    public Task DeleteFile(long fileId);

	public Task DeleteFolderItems(DeleteFolderItemsRequest request);
	public Task MoveFolderItems(MoveFolderItemsRequest request);
	public Task CopyFolderItems(CopyFolderItemsRequest request);

	public Task CreateFolder(CreateFolderRequest request);
    public Task UpdateFolder(UpdateFolderRequest request);
    public Task<FileSettings> GetFileSettings(long id);
    public Task UpdateFile(UpdateFileRequest request);
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
	public async Task UpdateFolder(UpdateFolderRequest request) => await _storageClient.UpdateFolderAsync(request);

    public async Task<FileSettings> GetFileSettings(long id) => await _storageClient.GetFileSettingsAsync(id);
	public async Task UpdateFile(UpdateFileRequest request) => await _storageClient.UpdateFileAsync(request);

	public async Task DeleteFolderItems(DeleteFolderItemsRequest request) => await _storageClient.DeleteFolderItemsAsync(request);

	public async Task MoveFolderItems(MoveFolderItemsRequest request) => await _storageClient.MoveFolderItemsAsync(request);

	public async Task CopyFolderItems(CopyFolderItemsRequest request) => await _storageClient.CopyFolderItemsAsync(request);
}
