using Concerto.Client.Extensions;
using Concerto.Shared.Models.Dto;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.JSInterop;
using MudBlazor;
using System.Diagnostics;
using System.Net.Http.Json;
using System.Text.Json;

namespace Concerto.Client.Services;

public interface IStorageService : IStorageClient
{
    public void QueueFilesToUpload(long folderId, IEnumerable<IBrowserFile> files);
    public void QueueFileToUpload(long folderId, IJSStreamReference file, string name);
    public void CancelAllUploads();
    public void ClearInactiveUploads();

    public Task DownloadFile(long fileId, string saveAs);
    public Task<string> GetFileUrl(long fileId, bool inline = false);

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
        var uploadQueueItems = files.Select(file => new BrowserFileUploadQueueItem(folderId, file));
        QueueFilesToUploadInternal(folderId, uploadQueueItems);
    }

    public void QueueFileToUpload(long folderId, IJSStreamReference file, string name)
    {
        var uploadQueueItem = new JsStreamFileUploadQueueItem(folderId, name, file);
        QueueFilesToUploadInternal(folderId, new[] { uploadQueueItem });
    }

    private void QueueFilesToUploadInternal(long folderId, IEnumerable<UploadQueueItem> files)
    {
        foreach (var file in files)
        {
            _uploadQueue.Enqueue(file);
            _items.Add(file);
        }

        if (!_isUploading)
        {
            _isUploading = true;
            ProcessUploadQueue().AndForget();
            _JS.InvokeVoidAsync("enablePreventWindowClose", "upload").AndForget();
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

        _JS.InvokeVoidAsync("disablePreventWindowClose", "upload").AndForget();
    }

    private async Task UploadQueueFile(UploadQueueItem item)
    {
        long chunkSize = 1024 * 1024 * 10;
        // long maxFileSize = _appSettingsService.AppSettings.FileSizeLimit;
        await item.InitializeAsync();
        Stream file = item.File;

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
            var url = await GetFileUrl(fileId);
            await _JS.InvokeVoidAsync("downloadFile", saveAs, $"{_http.BaseAddress}{url}");
        }
        catch (Exception e)
        {
            _snackbar.Add("Failed to initiate download.", Severity.Error);
            Console.WriteLine(e);
        }
    }

	public async Task<string> GetFileUrl(long fileId, bool inline = false)
	{
		var token = await GetFileDownloadTokenAsync(fileId);
		return $"Storage/DownloadFile?fileId={fileId}&token={token}&inline={inline}";
	}

}

public abstract class UploadQueueItem : IAsyncDisposable
{
    public long FolderId { get; private set; }
    public readonly CancellationTokenSource Cancellation = new();
    public abstract Stream File { get; }
    public abstract long Size { get; }
    public abstract string Name { get;}
    public FileUploadResult? Result { get; set; } = null;
    public double Progress { get; set; } = 0;

    public bool IsInProgress => Result == null && !IsCancelled;
    public bool IsCancelled { get; set; } = false;

    public bool IsError => Result != null && !Result.Uploaded;
    public bool IsComplete => Result != null && Result.Uploaded;
    public bool IsPending => IsInProgress && Progress == 0;
    public bool IsUploading => IsInProgress && Progress > 0;

    public UploadQueueItem(long folderId)
    {
        FolderId = folderId;
    }

    public abstract Task InitializeAsync();

    public virtual async ValueTask DisposeAsync()
    {
        await File.DisposeAsync();
        Cancellation.Dispose();
    }

}

public class BrowserFileUploadQueueItem : UploadQueueItem
{

    public override long Size { get; } 
    public override string Name { get; } 
    public override Stream File { get; }

    public BrowserFileUploadQueueItem(long folderId, IBrowserFile file) : base(folderId)
    {
        File = file.OpenReadStream(long.MaxValue);
        Size = file.Size;
        Name = file.Name;
    }
    public override Task InitializeAsync() => Task.CompletedTask;
}

public class JsStreamFileUploadQueueItem : UploadQueueItem
{
    public override long Size { get; } 
    public override string Name { get; } 

    IJSStreamReference jsStream;
    private Stream? _file;
    public override Stream File
    {
        get
        {
            if (_file == null) throw new InvalidOperationException("Stream not initialized");
            return _file;
        }
    }

    public JsStreamFileUploadQueueItem(long folderId, string name, IJSStreamReference file) : base(folderId)
    {
        Size = file.Length;
        Name = name;
        jsStream = file;
    }

    public override async Task InitializeAsync()
    {
        _file = await jsStream.OpenReadStreamAsync(long.MaxValue);
    }

    public override async ValueTask DisposeAsync()
    {
        await base.DisposeAsync();
        await jsStream.DisposeAsync();
    }
}


