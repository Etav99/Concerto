using Concerto.Server.Settings;
using Concerto.Shared.Models.Dto;
using Microsoft.AspNetCore.Mvc;

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
			AccountManagementUrl = AppSettings.IdentityProvider.AccountConsoleUrl,
			PostLogoutUrl = AppSettings.Oidc.ClientPostLogoutRedirectUrl,
			FileSizeLimit = AppSettings.Storage.FileSizeLimit,
			MaxAllowedFiles = AppSettings.Storage.MaxAllowedFiles,
		};
	}
}

