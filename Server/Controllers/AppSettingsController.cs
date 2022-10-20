using Concerto.Server.Extensions;
using Concerto.Server.Settings;
using Concerto.Shared.Models;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace Concerto.Server.Controllers;

[Route("[controller]/[action]")]
[ApiController]
public class AppSettingsController : ControllerBase
{

    [HttpGet]
    public ActionResult<ClientAppSettings> GetClientAppSettings()
    {
        return new ClientAppSettings
        {
            AuthorityUrl = AppSettings.Oidc.ClientAuthority,
            PostLogoutUrl = AppSettings.Oidc.ClientPostLogoutRedirectUrl
        };
    }
}
