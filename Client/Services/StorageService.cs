using Concerto.Client.Services;
using Concerto.Shared.Models.Dto;
using Microsoft.AspNetCore.Components.Forms;
using MudBlazor;
using static MudBlazor.CategoryTypes;
using static System.Net.WebRequestMethods;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Components;
using System.Net;
using System.IO;
using System.Net.Http.Json;
using System;

namespace Concerto.Client.Services;

public interface IStorageService : IStorageClient {
	public void QueueFilesToUpload(long folderId, IEnumerable<IBrowserFile> files);
	public EventHandler<IReadOnlyCollection<UploadQueueItem>>? QueueChangedEventHandler { get; set; }
}

public class StorageService : StorageClient, IStorageService
{
	ISnackbar _snackbar { get; set; }
	IAppSettingsService _appSettingsService { get; set; }
	
	HttpClient _http{ get; set; } = null!;
	
	private bool _isUploading = false;
	private Queue<UploadQueueItem> _uploadQueue = new();
	private List<UploadQueueItem> _items = new();
	public EventHandler<IReadOnlyCollection<UploadQueueItem>>? QueueChangedEventHandler { get; set; }


	public StorageService(HttpClient httpClient, IAppSettingsService appSettingsService, ISnackbar snackbar, HttpClient http) : base(httpClient)
	{
		_appSettingsService = appSettingsService;
		_snackbar = snackbar;
		_http = http;
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
		QueueChangedEventHandler?.Invoke(this, _items);
	}
	
	private async Task ProcessUploadQueue()
	{
		while(_uploadQueue.Count > 0)
		{
			var item = _uploadQueue.Dequeue();
			await UploadQueueFile(item);
			if(_uploadQueue.Count == 0)
			{
				_isUploading = false;
			}
		}
	}

	private async Task UploadQueueFile(UploadQueueItem item)
	{
		IBrowserFile file = item.File;
		using var content = new MultipartFormDataContent();
		var upload = false;

		long maxFileSize = _appSettingsService.AppSettings.FileSizeLimit;
		int maxFiles = _appSettingsService.AppSettings.MaxAllowedFiles;
		try
		{
			Action<long, double> onProgress = (bytes, percentage) => {
				item.Progress = percentage;
				QueueChangedEventHandler?.Invoke(this, _items);
			};

			// var fileStream = new StreamContent(file.OpenReadStream(maxFileSize));
			var fileStream = new ProgressableStreamContent(file.OpenReadStream(maxFileSize), 500000 , onProgress);
			// var fileStream = new ProgressiveStreamContent(file.OpenReadStream(maxFileSize), 500000 , onProgress);
			fileStream.Headers.ContentType = new MediaTypeHeaderValue(file.ContentType);
			// fileStream.Headers
			content.Add(fileStream, "\"files\"", file.Name);
			upload = true;
		}
		catch (Exception e)
		{
			item.Result = new FileUploadResult
			{
					DisplayFileName = file.Name,
					ErrorCode = 6,
					Uploaded = false,
					ErrorMessage = e.Message
			};
		}

		if (upload)
		{
			var response = await _http.PostAsync($"Storage/UploadFiles?folderId={item.FolderId}", content);

			var uploadResult = await response.Content.ReadFromJsonAsync<IEnumerable<FileUploadResult>>();
			item.Result = uploadResult?.FirstOrDefault();
		}

		if (item.Result == null || !item.Result.Uploaded)
		{
			_snackbar.Add(file.Name + " failed to upload: " + item.Result?.ErrorMessage, Severity.Error);
		}
	}

}


public class UploadQueueItem
{
	public long FolderId {get; private set;}
	public IBrowserFile File { get; private set; }
	public 	FileUploadResult? Result {get; set; }
	public double Progress {get; set;}
	public UploadQueueItem(long folderId, IBrowserFile file)
	{
		FolderId = folderId;
		File = file;
	}
}

public class ProgressableStreamContent : StreamContent
    {
        private const int DefaultBufferSize = 4096;
        private readonly int _bufferSize;
        private readonly Stream _streamToWrite;
        private bool _contentConsumed;

		private event Action<long, double> _onProgress;

        public ProgressableStreamContent(Stream streamToWrite, Action<long, double> onProgress) : this(streamToWrite, DefaultBufferSize, onProgress) {}

        public ProgressableStreamContent(Stream streamToWrite, int bufferSize, Action<long, double> onProgress)
            : base(streamToWrite, bufferSize)
        {
            if (streamToWrite == null)
            {
                throw new ArgumentNullException("streamToWrite");
            }

            if (bufferSize <= 0)
            {
                throw new ArgumentOutOfRangeException("bufferSize");
            }

            _streamToWrite = streamToWrite;
            _bufferSize = bufferSize;
			_onProgress = onProgress;
        }


        protected override void Dispose(bool disposing)
        {
            if (disposing) _streamToWrite.Dispose();
            base.Dispose(disposing);
        }
        protected override async Task SerializeToStreamAsync(Stream stream, TransportContext? context)
        {
            PrepareContent();

            var buffer = new byte[_bufferSize];
            long size = _streamToWrite.Length;
            long uploaded = 0;

            using (_streamToWrite)
            {
                while (true)
                {
                    var length = await _streamToWrite.ReadAsync(buffer, 0, buffer.Length);
                    if (length <= 0) break;
                    uploaded += length;
                    await stream.WriteAsync(buffer, 0, length);
					await stream.FlushAsync();

					var percantage = Convert.ToDouble(uploaded * 100 / size);
					_onProgress?.Invoke(uploaded, percantage);
                }
            }
        }

        protected override bool TryComputeLength(out long length)
        {
            length = _streamToWrite.Length;
            return true;
        }

        private void PrepareContent()
        {
            if (_contentConsumed)
            {
                // If the content needs to be written to a target stream a 2nd time, then the stream must support
                // seeking (e.g. a FileStream), otherwise the stream can't be copied a second time to a target 
                // stream (e.g. a NetworkStream).
                if (_streamToWrite.CanSeek)
                {
                    _streamToWrite.Position = 0;
                }
                else
                {
                    throw new InvalidOperationException("The stream has already been read.");
                }
            }

            _contentConsumed = true;
        }
    }

  