using GPAHub.Application.DTOs.Course;
using GPAHub.Application.DTOs.Semester;
using GPAHub.Application.Interfaces.Services;
using GPAHub.Web.Controllers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GPAHub.Web.Controllers;

[Authorize]
[Route("api/semesters")]
public class SemestersController : ApiControllerBase
{
    private readonly ISemesterService _semesterService;

    public SemestersController(ISemesterService semesterService)
    {
        _semesterService = semesterService;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<SemesterOptionDto>>> List(CancellationToken cancellationToken) =>
        FromResult(await _semesterService.ListByStudentAsync(RequireStudentId(), cancellationToken));

    [HttpPost]
    public async Task<ActionResult<SemesterOptionDto>> Create(CreateSemesterDto dto, CancellationToken cancellationToken) =>
        FromResult(await _semesterService.CreateAsync(RequireStudentId(), dto, cancellationToken));

    [HttpPut("{semesterId:guid}")]
    public async Task<IActionResult> Rename(Guid semesterId, UpdateSemesterDto dto, CancellationToken cancellationToken) =>
        FromResult(await _semesterService.RenameAsync(RequireStudentId(), semesterId, dto, cancellationToken));

    [HttpDelete("{semesterId:guid}")]
    public async Task<IActionResult> Delete(Guid semesterId, CancellationToken cancellationToken) =>
        FromResult(await _semesterService.DeleteAsync(RequireStudentId(), semesterId, cancellationToken));
}
