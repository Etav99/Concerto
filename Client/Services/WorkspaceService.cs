using Concerto.Shared.Models.Dto;

namespace Concerto.Client.Services;

public interface IWorkspaceService : IWorkspaceClient
{
    public EventHandler<IEnumerable<WorkspaceListItem>>? UserWorkspacesFetchEventHandler { get; set; }
}

public class WorkspaceService : WorkspaceClient, IWorkspaceService
{
    public WorkspaceService(HttpClient httpClient) : base(httpClient) { }

    public EventHandler<IEnumerable<WorkspaceListItem>>? UserWorkspacesFetchEventHandler { get; set; }

    public override async Task<ICollection<WorkspaceListItem>> GetCurrentUserWorkspacesAsync()
    {
        var workspaces = await base.GetCurrentUserWorkspacesAsync();
        UserWorkspacesFetchEventHandler?.Invoke(this, workspaces);
        return workspaces;
    }
}

public interface ISessionService : ISessionClient { }

public class SessionService : SessionClient, ISessionService
{
    public SessionService(HttpClient httpClient) : base(httpClient) { }
}


