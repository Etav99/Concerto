using Concerto.Server.Settings;
using Concerto.Server.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Concerto.Shared.Extensions;

namespace Concerto.Server.Pages;

[Authorize(AuthenticationSchemes = OpenIdConnectDefaults.AuthenticationScheme)]
public class MeetingAuthorizationModel : PageModel
{
    private readonly SessionService _sessionService;
    private readonly ILogger<MeetingAuthorizationModel> _logger;
    public MeetingAuthorizationModel(ILogger<MeetingAuthorizationModel> logger, SessionService sessionService)
    {
        _sessionService = sessionService;
        _logger = logger;
    }

    public async Task<IActionResult> OnGetAsync(string roomGuid)
    {
		var userId = HttpContext.User.GetSubjectId();
		var meetingGuid = Guid.Parse(roomGuid);
		if (!await _sessionService.CanAccessSession(userId, meetingGuid))
			return Forbid();

		try
		{
			var token = await _sessionService.GenerateMeetingToken(userId, meetingGuid);
			return Redirect($"{AppSettings.Meetings.JitsiUrl}/{roomGuid}?jwt={token}");
		}
		catch
		{
			return BadRequest();
		}
	}
}
