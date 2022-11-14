﻿using Concerto.Server.Middlewares;
using Concerto.Server.Services;
using Concerto.Shared.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Concerto.Server.Controllers;

[Route("[controller]/[action]")]
[ApiController]
[Authorize]
public class CourseController : ControllerBase
{
	private readonly ILogger<CourseController> _logger;
	private readonly CourseService _courseService;


	public CourseController(ILogger<CourseController> logger, CourseService courseService)
	{
		_logger = logger;
		_courseService = courseService;
	}

	[HttpGet]
	public async Task<IEnumerable<Dto.Course>> GetCurrentUserCourses()
	{
        long userId = HttpContext.GetUserId();
		return await _courseService.GetUserCourses(userId);
	}

	[HttpGet]
	public async Task<ActionResult<Dto.Course>> GetCourse(long courseId)
	{
        long userId = HttpContext.GetUserId();
        
		if (!await _courseService.IsUserCourseMember(userId, courseId)) return Forbid();

		var course = await _courseService.GetCourse(courseId);
        if (course == null) return NotFound();
        return Ok(course);
    }

    [Authorize(Roles = "teacher")]
    [HttpPost]
	public async Task<ActionResult> CreateCourseForCurrentUser([FromBody] Dto.CreateCourseRequest request)
	{
        long userId = HttpContext.GetUserId();

        if (request.Members.Count() != request.Members.DistinctBy(x => x.UserId).Count()) return BadRequest("Duplicate members");
        
        if (await _courseService.CreateCourse(request, userId)) return Ok();
		return BadRequest();
	}

    [Authorize(Roles = "teacher")]
    [HttpDelete]
    public async Task<ActionResult> DeleteCourse(long courseId)
	{
        long userId = HttpContext.GetUserId();

		if (!User.IsInRole("admin") && !await _courseService.CanDeleteCourse(courseId, userId)) return Forbid();
        
        if (await _courseService.DeleteCourse(courseId, userId)) return Ok();
        
        return BadRequest();
    }
}