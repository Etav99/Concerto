using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication.Internal;
using System.Security.Claims;
using System.Text.Json;

namespace Concerto.Client.Extensions;

public class CustomAccountFactory : AccountClaimsPrincipalFactory<RemoteUserAccount>
{
    public CustomAccountFactory(IAccessTokenProviderAccessor accessor)
        : base(accessor) { }

    public override async ValueTask<ClaimsPrincipal> CreateUserAsync(RemoteUserAccount account, RemoteAuthenticationUserOptions options)
    {
        var initialUser = await base.CreateUserAsync(account, options);
        if (!(initialUser.Identity?.IsAuthenticated ?? false)) return initialUser;

        var userIdentity = (ClaimsIdentity)initialUser.Identity;
        var roles = userIdentity.Claims.FirstOrDefault(c => c.Type == "roles")?.Value;
        if (string.IsNullOrEmpty(roles)) return initialUser;

        var rolesNode = JsonDocument.Parse(roles);
        foreach (var role in rolesNode.RootElement.EnumerateArray())
        {
            var value = role.GetString();
            if (!string.IsNullOrEmpty(value)) userIdentity.AddClaim(new Claim("role", value));
        }

        return initialUser;
    }
}


