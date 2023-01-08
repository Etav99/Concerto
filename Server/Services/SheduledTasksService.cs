namespace Concerto.Server.Services;

public class SheduledTasksService : IHostedService, IDisposable
{
	private int executionCount = 0;
	private readonly ILogger<SheduledTasksService> _logger;


	private Timer? OneTimeTokenCleanupTimer = null;

	public SheduledTasksService(ILogger<SheduledTasksService> logger)
	{
		_logger = logger;
	}

	public Task StartAsync(CancellationToken stoppingToken)
	{

/*		OneTimeTokenCleanupTimer = new Timer(DoWork, null, TimeSpan.Zero,
			TimeSpan.FromSeconds(5));*/

		return Task.CompletedTask;
	}

	public Task StopAsync(CancellationToken stoppingToken)
	{
		_logger.LogInformation("Timed Hosted Service is stopping.");

		OneTimeTokenCleanupTimer?.Change(Timeout.Infinite, 0);

		return Task.CompletedTask;
	}

	public void Dispose()
	{
		OneTimeTokenCleanupTimer?.Dispose();
	}

}
