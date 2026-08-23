using GPAHub.Application.DTOs.Student;
using GPAHub.Application.Interfaces.Services;
using GPAHub.Web.Controllers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GPAHub.Web.Controllers;

[Authorize]
[Route("api/students")]
public class StudentsController : ApiControllerBase
{
    private readonly IAcademicRecordService _academicRecordService;

    public StudentsController(IAcademicRecordService academicRecordService)
    {
        _academicRecordService = academicRecordService;
    }

    [HttpGet("profile")]
    [ProducesResponseType(typeof(StudentProfileDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<StudentProfileDto>> GetProfile(CancellationToken cancellationToken) =>
        FromResult(await _academicRecordService.GetProfileAsync(RequireStudentId(), cancellationToken));

    [HttpPut("profile")]
    public async Task<ActionResult<StudentProfileDto>> UpdateProfile(UpdateProfileDto dto, CancellationToken cancellationToken) =>
        FromResult(await _academicRecordService.UpdateProfileAsync(RequireStudentId(), dto, cancellationToken));

    [HttpGet("baseline")]
    [ProducesResponseType(typeof(AcademicBaselineDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<AcademicBaselineDto>> GetBaseline(CancellationToken cancellationToken) =>
        FromResult(await _academicRecordService.GetBaselineAsync(RequireStudentId(), cancellationToken));

    [HttpPut("baseline")]
    public async Task<ActionResult<AcademicBaselineDto>> UpdateBaseline(UpdateBaselineDto dto, CancellationToken cancellationToken) =>
        FromResult(await _academicRecordService.UpdateBaselineAsync(RequireStudentId(), dto, cancellationToken));

    [HttpDelete("baseline")]
    public async Task<IActionResult> ClearBaseline(CancellationToken cancellationToken) =>
        FromResult(await _academicRecordService.ClearBaselineAsync(RequireStudentId(), cancellationToken));
}
