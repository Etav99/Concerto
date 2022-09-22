using Concerto.Shared.Extensions;
using Microsoft.AspNetCore.SignalR;

namespace Concerto.Server.Hubs
{
    public class UserIdProvider : IUserIdProvider
    {
        public string? GetUserId(HubConnectionContext connection)
        {
            return connection.User?.GetUserIdString();
        }
    }
}
