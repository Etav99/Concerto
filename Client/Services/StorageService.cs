using Concerto.Client.Extensions;
using Concerto.Shared.Models.Dto;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
using Microsoft.JSInterop;
using MudBlazor;
using System.Diagnostics;
using System.Net.Http.Json;
using System.Text.Json;
using static MudBlazor.CategoryTypes;
using static System.Net.WebRequestMethods;

namespace Concerto.Client.Services;

public interface IStorageService : IStorageClient
{
    public void QueueFilesToUpload(long folderId, IEnumerable<IBrowserFile> files);
    public void CancelAllUploads();
    public void ClearInactiveUploads();

    public Task DownloadFile(long fileId, string saveAs);

    public void ClearIfInactive(UploadQueueItem item);
    public EventHandler<IReadOnlyCollection<UploadQueueItem>>? QueueChangedEventHandler { get; set; }
    public EventHandler<IReadOnlyCollection<UploadQueueItem>>? NewQueueItemsEventHandler { get; set; }
    public EventHandler<long>? OnUploadedToFolderEventHandler { get; set; }
}

public class StorageService : StorageClient, IStorageService
{
    ISnackbar _snackbar { get; set; }
    IAppSettingsService _appSettingsService { get; set; }
    IDialogService _dialogService { get; set; }
    IJSRuntime _JS { get; set; }
    HttpClient _http { get; set; } = null!;

    private bool _isUploading = false;
    private Queue<UploadQueueItem> _uploadQueue = new();
    private List<UploadQueueItem> _items = new();
    public EventHandler<IReadOnlyCollection<UploadQueueItem>>? QueueChangedEventHandler { get; set; }
    public EventHandler<IReadOnlyCollection<UploadQueueItem>>? NewQueueItemsEventHandler { get; set; }
    public EventHandler<long>? OnUploadedToFolderEventHandler { get; set; }

    public StorageService(
        HttpClient httpClient,
        IAppSettingsService appSettingsService,
        ISnackbar snackbar,
        HttpClient http,
        IDialogService dialogService,
        IJSRuntime jS
    ) : base(httpClient)
    {
        _appSettingsService = appSettingsService;
        _snackbar = snackbar;
        _http = http;
        _dialogService = dialogService;
        _JS = jS;
    }

	public void QueueFilesToUpload(long folderId, IEnumerable<IBrowserFile> files)
    {
        foreach (var file in files)
        {
            var queueItem = new UploadQueueItem(folderId, file);
            _uploadQueue.Enqueue(queueItem);
            _items.Add(queueItem);
        }

        if (!_isUploading)
        {
            _isUploading = true;
            ProcessUploadQueue().AndForget();
        }

        NewQueueItemsEventHandler?.Invoke(this, _items);
    }

    private async Task ProcessUploadQueue()
    {
        List<UploadQueueItem> processedItems = new();
        Stopwatch stopwatch = new();
        stopwatch.Start();
        bool anyUploaded = false;
        while (_uploadQueue.Count > 0)
        {
            var item = _uploadQueue.Dequeue();
            await UploadQueueFile(item);
            processedItems.Add(item);

            if (item.Result?.Uploaded ?? false)
                anyUploaded = true;

            if (_uploadQueue.Count == 0)
                _isUploading = false;

            if ((!_isUploading || stopwatch.ElapsedMilliseconds > 2000) && item.IsComplete)
            {
                stopwatch.Restart();
                OnUploadedToFolderEventHandler?.Invoke(this, item.FolderId);
            }

        }
        stopwatch.Stop();

        var errors = processedItems
            .Where(pi => pi.IsError)
            .Select(pi => $"{pi.Name}: {pi.Result?.ErrorMessage ?? "Unknown error"} (Error code {pi.Result?.ErrorCode ?? -1})")
            .ToArray();

        if (errors.Any()) await _dialogService.ShowInfoDialog("Some files couldn't be uploaded: \n", string.Join("\n", errors));
        else if (anyUploaded) _snackbar.Add("Files uploaded", Severity.Success);
    }

    private async Task UploadQueueFile(UploadQueueItem item)
    {
        long chunkSize = 1024 * 1024 * 10;
        Stream file = item.File;
        // long maxFileSize = _appSettingsService.AppSettings.FileSizeLimit;

        FileChunkMetadata? fileChunk = null;
        try
        {
            long size = item.Size;
            long uploaded = 0;

            var guid = Guid.NewGuid();
            var buffer = new byte[chunkSize];

            using var cancellation = item.Cancellation;
            await using var fileStream = item.File;
            
            while (uploaded < size)
            {
                var readBytes = await fileStream.ReadAsync(buffer, 0, buffer.Length, cancellation.Token);
                cancellation.Token.ThrowIfCancellationRequested();
                fileChunk = new FileChunkMetadata { FileSize = size, FolderId = item.FolderId, Guid = guid, Offset = uploaded, };

                var chunkContent = new MultipartFormDataContent();
                chunkContent.Add(new ByteArrayContent(buffer, 0, readBytes), "file", item.Name);
                chunkContent.Add(JsonContent.Create(fileChunk, options: JsonSerializerOptions.Default), "chunk");

                cancellation.Token.ThrowIfCancellationRequested();
                var response = await _http.PostAsync($"Storage/UploadFileChunk", chunkContent, cancellation.Token);
                response.EnsureSuccessStatusCode();

                uploaded += readBytes;
                item.Progress = Convert.ToDouble((int)(uploaded * 100 / size));

                if (fileStream.Position == fileStream.Length)
                {
                    item.Result = await response.Content.ReadFromJsonAsync<FileUploadResult>();
                    ;
                    item.Progress = 100;
                }

                QueueChangedEventHandler?.Invoke(this, _items);
            }
        }
        catch (Exception e) when (e is OperationCanceledException or TaskCanceledException)
        {
            if (fileChunk is not null) await AbortFileUploadAsync(fileChunk);
            item.IsCancelled = true;
        }
        catch (Exception e)
        {
            item.Result = new FileUploadResult { DisplayFileName = item.Name, ErrorCode = 6, Uploaded = false, ErrorMessage = e.Message };
        }
        finally
        {
            QueueChangedEventHandler?.Invoke(this, _items);
        }
    }

    public void CancelAllUploads()
    {
        foreach (var item in _items)
            item.Cancellation.Cancel();
    }

    public void ClearInactiveUploads()
    {
        var inactiveItems = _items.RemoveAll(i => !i.IsInProgress);
        QueueChangedEventHandler?.Invoke(this, _items);
    }

    public void ClearIfInactive(UploadQueueItem item)
    {
        if (!item.IsInProgress) _items.Remove(item);
        QueueChangedEventHandler?.Invoke(this, _items);
    }

    public async Task DownloadFile(long fileId, string saveAs)
    {
        try
        {
            var token = await GetOneTimeTokenAsync(fileId);
            var url = $"Storage/DownloadFile?fileId={fileId}&token={token}";
            await _JS.InvokeVoidAsync("downloadFile", saveAs, $"{_http.BaseAddress}{url}");
        }
        catch
        {
            _snackbar.Add("Failed to initiate download.", Severity.Error);
        }
    }

}

public class UploadQueueItem
{
    public long FolderId { get; private set; }
    public readonly CancellationTokenSource Cancellation = new();
    public Stream File { get; private set; }
    public long Size { get; private set; }
    public string Name { get; private set; }
    public FileUploadResult? Result { get; set; } = null;
    public double Progress { get; set; } = 0;

    public bool IsInProgress => Result == null && !IsCancelled;
    public bool IsCancelled { get; set; } = false;

    public bool IsError => Result != null && !Result.Uploaded;
    public bool IsComplete => Result != null && Result.Uploaded;
    public bool IsPending => IsInProgress && Progress == 0;
    public bool IsUploading => IsInProgress && Progress > 0;

    public UploadQueueItem(long folderId, IBrowserFile file)
    {
        FolderId = folderId;
        Size = file.Size;
        Name = file.Name;
        File = file.OpenReadStream(long.MaxValue);
    }
}
