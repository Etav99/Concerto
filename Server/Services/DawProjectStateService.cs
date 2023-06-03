using Nito.AsyncEx;
using System.Collections.Concurrent;
using System.Diagnostics.Metrics;
using System.Security.Cryptography;
using System.Threading;

namespace Concerto.Server.Services;


public class DawProjectStateService
{
    private readonly ILogger<DawProjectStateService> _logger;

    public DawProjectStateService(ILogger<DawProjectStateService> logger)
    {
        _logger = logger;
    }


    public ConcurrentDictionary<(long projectId, long userId), int> ProjectUserConnections { get; } =  new();
    private object projectLocksLock = new();

    private AsyncLock ProjectGenerationLock = new();
    public Dictionary<long, SemaphoreSlim> ProjectGenerationAsyncLocks { get; } =  new();

    public void AddUserConnection(long projectId, long userId)
    {
        var id = (projectId, userId);
        ProjectUserConnections.AddOrUpdate(id, 1, (_, count) => count + 1);
    }

    public async Task<SemaphoreSlim> GetProjectGenerationLock(long projectId)
    {
        using(await ProjectGenerationLock.LockAsync())
        {
            ProjectGenerationAsyncLocks.TryGetValue(projectId, out var projectLock);
            if (projectLock is not null)
            {
                return projectLock;
            }
            else
            {
                var newLock = new SemaphoreSlim(1, 1);
                ProjectGenerationAsyncLocks.Add(projectId, newLock);
                return newLock;
            }
        }
    }

    public async Task ReturnProjectGenerationLock(long projectId)
    {
        using(await ProjectGenerationLock.LockAsync())
        {
            if (!ProjectGenerationAsyncLocks.TryGetValue(projectId, out var projectLock))
                return;

            if (projectLock.CurrentCount == 0)
                return;

            ProjectGenerationAsyncLocks.Remove(projectId);
        }
    }

    public void RemoveUserConnection(long projectId, long userId)
    {
        var id = (projectId, userId);
        //Decrement and remove if 0
        ProjectGenerationAsyncLocks.TryGetValue(projectId, out var asyncLock);
        var newValue = ProjectUserConnections.AddOrUpdate(id, 0, (_, count) => count - 1);
        if (newValue == 0)
        {
            ProjectUserConnections.TryRemove(new KeyValuePair<(long, long), int>(id, 0));
        }
    }

}