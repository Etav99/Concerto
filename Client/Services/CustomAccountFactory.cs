using Microsoft.AspNetCore.Components.WebAssembly.Authentication.Internal;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
using System.Security.Claims;
using System.Text.Json;

namespace Concerto.Client.Services;

public class CustomAccountFactory : AccountClaimsPrincipalFactory<RemoteUserAccount>
{
    public CustomAccountFactory(IAccessTokenProviderAccessor accessor)
        : base(accessor)
    {
    }

    public async override ValueTask<ClaimsPrincipal> CreateUserAsync(
                RemoteUserAccount account, RemoteAuthenticationUserOptions options)
    {
        var initialUser = await base.CreateUserAsync(account, options);

        if (initialUser.Identity?.IsAuthenticated ?? false)
        {
            var userIdentity = (ClaimsIdentity)initialUser.Identity;
            var roles = userIdentity.Claims.First(c => c.Type == "roles").Value;
            var rolesNode = JsonDocument.Parse(roles);
      
            foreach (var role in rolesNode.RootElement.EnumerateArray())
            {
                string? value = role.GetString();
                if (!string.IsNullOrEmpty(value))
                {
                    userIdentity.AddClaim(new Claim("role", value));
                }

            }
        }
        
        return initialUser;
    }
}