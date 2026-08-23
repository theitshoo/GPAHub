using GPAHub.Application.DTOs.Course;
using GPAHub.Application.Interfaces.Services;
using GPAHub.Web.Controllers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GPAHub.Web.Controllers;

[Authorize]
[Route("api/courses")]
public class CoursesController : ApiControllerBase
{
    private readonly ICourseService _courseService;

    public CoursesController(ICourseService courseService)
    {
        _courseService = courseService;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<CourseDto>>> List([FromQuery] Guid? semesterId, CancellationToken cancellationToken) =>
        FromResult(await _courseService.ListByStudentAsync(RequireStudentId(), semesterId, cancellationToken));

    [HttpGet("{courseId:guid}")]
    public async Task<ActionResult<CourseDto>> GetById(Guid courseId, CancellationToken cancellationToken) =>
        FromResult(await _courseService.GetByIdAsync(RequireStudentId(), courseId, cancellationToken));

    [HttpPost]
    public async Task<ActionResult<CourseDto>> Create(CourseInputDto dto, CancellationToken cancellationToken)
    {
        var result = await _courseService.CreateAsync(RequireStudentId(), dto, cancellationToken);

        return result.IsSuccess
            ? CreatedAtAction(nameof(GetById), new { courseId = result.Value.Id }, result.Value)
            : FromError(result.Error!);
    }

    [HttpPut("{courseId:guid}")]
    public async Task<ActionResult<CourseDto>> Update(Guid courseId, CourseInputDto dto, CancellationToken cancellationToken) =>
        FromResult(await _courseService.UpdateAsync(RequireStudentId(), courseId, dto, cancellationToken));

    [HttpDelete("{courseId:guid}")]
    public async Task<IActionResult> Delete(Guid courseId, CancellationToken cancellationToken) =>
        FromResult(await _courseService.DeleteAsync(RequireStudentId(), courseId, cancellationToken));
}
