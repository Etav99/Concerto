using Concerto.Server.Extensions;
using Concerto.Server.Settings;

namespace Concerto.Server.Services;

public class ScheduledTasksService : IHostedService, IDisposable
{
	private readonly ILogger<ScheduledTasksService> _logger;
	private Timer? _tempFilesCleanupTimer = null;

	public ScheduledTasksService(ILogger<ScheduledTasksService> logger)
	{
		_logger = logger;
	}

	public Task StartAsync(CancellationToken stoppingToken)
	{

		_tempFilesCleanupTimer = new Timer(RemoveExpiredTempFiles, null, TimeSpan.Zero, AppSettings.Storage.TempFileExpirationSpan);

		return Task.CompletedTask;
	}

	public Task StopAsync(CancellationToken stoppingToken)
	{
		_logger.LogInformation("Scheduled Tasks Service is stopping.");
		_tempFilesCleanupTimer?.Change(Timeout.Infinite, 0);
		return Task.CompletedTask;
	}

	public void Dispose()
	{
		_tempFilesCleanupTimer?.Dispose();
	}
	
	private async void RemoveExpiredTempFiles(object? state) {
		try
		{
			var tempDir = new DirectoryInfo(AppSettings.Storage.TempPath);
			if (!tempDir.Exists) return;
			var expirationSpan = AppSettings.Storage.TempFileExpirationSpan;
			var files = tempDir.GetFiles();

			foreach (var file in files)
			{
				if(file.LastWriteTimeUtc < DateTime.UtcNow - expirationSpan) {
					await file.DeleteAsync();
				}
			}
		}
		catch(Exception ex)
		{
			_logger.LogError(ex, "Error while removing expired temp files.");
		}
	}

}
