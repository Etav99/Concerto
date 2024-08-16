using Microsoft.AspNetCore.Components;

namespace Concerto.Client.Extensions;

public static class NavigationManagerExtensions
{
    public static void ToWorkspace(this NavigationManager navigation, long workspaceId, string tab)
    {
        navigation.NavigateTo($"workspaces/{workspaceId}/{tab}");
    }
}



