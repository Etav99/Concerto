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

		Guid meetingGuid;
		try
		{
			meetingGuid = Guid.Parse(roomGuid);
		}
		catch
		{
			return Redirect($"{AppSettings.Web.BasePath}");
		}

		if (!await _sessionService.CanAccessSession(meetingGuid,userId))
			return Forbid();

		try
		{
			var ids = await _sessionService.GetCourseAndSessionIds(meetingGuid);
			return Redirect($"{AppSettings.Web.BasePath}/courses/{ids.courseId}/sessions/{ids.sessionId}");
		}
		catch
		{
			return BadRequest();
		}
	}
}
