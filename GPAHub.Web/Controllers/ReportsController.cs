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
    private readonly IPdfReportGenerator _pdfReportGenerator;

    public ReportsController(IReportService reportService, IPdfReportGenerator pdfReportGenerator)
    {
        _reportService = reportService;
        _pdfReportGenerator = pdfReportGenerator;
    }

    [HttpGet("gpa-records/{recordId:guid}")]
    [ProducesResponseType(typeof(ReportDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<ReportDto>> GpaReport(Guid recordId, CancellationToken cancellationToken) =>
        FromResult(await _reportService.BuildGpaReportAsync(RequireStudentId(), recordId, cancellationToken));

    [HttpGet("gpa-records/{recordId:guid}/pdf")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GpaReportPdf(Guid recordId, CancellationToken cancellationToken)
    {
        var result = await _reportService.BuildGpaReportAsync(RequireStudentId(), recordId, cancellationToken);

        return result.IsSuccess
            ? File(_pdfReportGenerator.Generate(result.Value), "application/pdf",
                $"gpahub-gpa-report-{recordId:N}.pdf")
            : FromError(result.Error!);
    }

    [HttpGet("target-plans/{planId:guid}")]
    public async Task<ActionResult<ReportDto>> TargetReport(Guid planId, CancellationToken cancellationToken) =>
        FromResult(await _reportService.BuildTargetReportAsync(RequireStudentId(), planId, cancellationToken));

    [HttpGet("target-plans/{planId:guid}/pdf")]
    public async Task<IActionResult> TargetReportPdf(Guid planId, CancellationToken cancellationToken)
    {
        var result = await _reportService.BuildTargetReportAsync(RequireStudentId(), planId, cancellationToken);

        return result.IsSuccess
            ? File(_pdfReportGenerator.Generate(result.Value), "application/pdf",
                $"gpahub-target-report-{planId:N}.pdf")
            : FromError(result.Error!);
    }
}
