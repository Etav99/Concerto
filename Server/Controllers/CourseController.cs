using Concerto.Server.Extensions;
using Concerto.Server.Middlewares;
using Concerto.Server.Services;
using Concerto.Shared.Extensions;
using Concerto.Shared.Models.Dto;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Concerto.Server.Controllers;

[Route("[controller]/[action]")]
[ApiController]
[Authorize]
public class CourseController : ControllerBase
{
	private readonly CourseService _courseService;
	private readonly ILogger<CourseController> _logger;


	public CourseController(ILogger<CourseController> logger, CourseService courseService)
	{
		_logger = logger;
		_courseService = courseService;
	}

	private long UserId => HttpContext.UserId();

	[HttpGet]
	public async Task<ActionResult<IEnumerable<CourseListItem>>> GetCurrentUserCourses()
	{
		if (User.IsAdmin()) return Ok(await _courseService.GetAllCourses());
		return Ok(await _courseService.GetUserCoursesList(UserId));
	}

	[HttpGet]
	public async Task<ActionResult<Course>> GetCourse(long courseId)
	{
		var isAdmin = User.IsAdmin();
		if (isAdmin || await _courseService.IsUserCourseMember(UserId, courseId))
		{
			var course = await _courseService.GetCourse(courseId, UserId, isAdmin);
			if (course == null) return NotFound();
			return Ok(course);
		}

		return Forbid();
	}

	[HttpGet]
	public async Task<ActionResult<CourseSettings>> GetCourseSettings(long courseId)
	{
		var isAdmin = User.IsAdmin();
		if (isAdmin || await _courseService.CanManageCourse(courseId, UserId))
		{
			var courseSettings = await _courseService.GetCourseSettings(courseId, UserId, isAdmin);
			if (courseSettings == null) return NotFound();
			return Ok(courseSettings);
		}

		return Forbid();
	}

	[HttpGet]
	public async Task<ActionResult<IEnumerable<User>>> GetCourseUsers(long courseId)
	{
		if (User.IsAdmin() || await _courseService.IsUserCourseMember(UserId, courseId))
		{
			var courseUsers = await _courseService.GetCourseUsers(courseId);
			return Ok(courseUsers);
		}

		return Forbid();
	}

	[Authorize(Roles = "teacher")]
	[HttpPost]
	[ProducesResponseType(StatusCodes.Status201Created)]
	[ProducesResponseType(StatusCodes.Status400BadRequest)]
	public async Task<ActionResult<long>> CreateCourseForCurrentUser([FromBody] CreateCourseRequest request)
	{
		if (request.Members.Count() != request.Members.DistinctBy(x => x.UserId).Count()) return BadRequest("Duplicate members");

		var newCourseId = await _courseService.CreateCourse(request, UserId);
		if (newCourseId > 0) return CreatedAtAction("GetCourse", new { courseId = newCourseId }, newCourseId);
		return BadRequest();
	}

	[Authorize(Roles = "teacher")]
	[HttpPost]
	public async Task<ActionResult<long>> CloneCourse([FromBody] CloneCourseRequest request)
	{
		if (!await _courseService.IsUserCourseMember(UserId, request.CourseId)) return Forbid();
		var newCourseId = await _courseService.CloneCourse(request, UserId);
		if (newCourseId <= 0) BadRequest();
		return Ok(newCourseId);
	}

	[Authorize(Roles = "teacher")]
	[HttpPost]
	public async Task<ActionResult> UpdateCourse([FromBody] UpdateCourseRequest request)
	{
		if (!User.IsAdmin() && !await _courseService.CanManageCourse(request.CourseId, UserId)) return Forbid();

		if (await _courseService.UpdateCourse(request, UserId)) return Ok();
		return BadRequest();
	}

	[Authorize(Roles = "teacher")]
	[HttpDelete]
	public async Task<ActionResult> DeleteCourse(long courseId)
	{
		if (!User.IsAdmin() && !await _courseService.CanDeleteCourse(courseId, UserId)) return Forbid();

		if (await _courseService.DeleteCourse(courseId)) return Ok();

		return BadRequest();
	}

	[HttpGet]
	public async Task<ActionResult<bool>> CanManageCourseSessions(long courseId)
	{
		return Ok(User.IsAdmin() || await _courseService.CanManageCourseSessions(courseId, UserId));
	}
	
}
