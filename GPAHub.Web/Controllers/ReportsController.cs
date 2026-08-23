using GPAHub.Application.DTOs.Report;
using GPAHub.Application.Interfaces.Services;
using GPAHub.Web.Controllers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GPAHub.Web.Controllers;

[Authorize]
[Route("api/reports")]
public class ReportsController : ApiControllerBase
{
    private readonly IReportService _reportService;

    public ReportsController(IReportService reportService)
    {
        _reportService = reportService;
    }

    [HttpGet("gpa-records/{recordId:guid}")]
    [ProducesResponseType(typeof(ReportDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<ReportDto>> GpaReport(Guid recordId, CancellationToken cancellationToken) =>
        FromResult(await _reportService.BuildGpaReportAsync(RequireStudentId(), recordId, cancellationToken));

    [HttpGet("target-plans/{planId:guid}")]
    public async Task<ActionResult<ReportDto>> TargetReport(Guid planId, CancellationToken cancellationToken) =>
        FromResult(await _reportService.BuildTargetReportAsync(RequireStudentId(), planId, cancellationToken));
}
