using GPAHub.Application.Common;
using GPAHub.Application.DTOs.History;
using GPAHub.Application.Interfaces.Services;
using GPAHub.Web.Controllers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GPAHub.Web.Controllers;

[Authorize]
[Route("api/history")]
public class HistoryController : ApiControllerBase
{
    private readonly IHistoryService _historyService;

    public HistoryController(IHistoryService historyService)
    {
        _historyService = historyService;
    }

    [HttpGet("gpa-records")]
    public async Task<ActionResult<PagedResultDto<GpaRecordSummaryDto>>> ListGpaRecords(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default) =>
        FromResult(await _historyService.ListGpaRecordsAsync(
            RequireStudentId(), new HistoryPageRequest(page, pageSize), cancellationToken));

    [HttpGet("gpa-records/{recordId:guid}")]
    public async Task<ActionResult<GpaRecordDetailDto>> GetGpaRecord(Guid recordId, CancellationToken cancellationToken) =>
        FromResult(await _historyService.GetGpaRecordAsync(RequireStudentId(), recordId, cancellationToken));

    [HttpDelete("gpa-records/{recordId:guid}")]
    public async Task<IActionResult> DeleteGpaRecord(Guid recordId, CancellationToken cancellationToken) =>
        FromResult(await _historyService.DeleteGpaRecordAsync(RequireStudentId(), recordId, cancellationToken));

    [HttpGet("target-plans")]
    public async Task<ActionResult<PagedResultDto<TargetPlanSummaryDto>>> ListTargetPlans(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default) =>
        FromResult(await _historyService.ListTargetPlansAsync(
            RequireStudentId(), new HistoryPageRequest(page, pageSize), cancellationToken));

    [HttpGet("target-plans/{planId:guid}")]
    public async Task<ActionResult<TargetPlanDetailDto>> GetTargetPlan(Guid planId, CancellationToken cancellationToken) =>
        FromResult(await _historyService.GetTargetPlanAsync(RequireStudentId(), planId, cancellationToken));

    [HttpDelete("target-plans/{planId:guid}")]
    public async Task<IActionResult> DeleteTargetPlan(Guid planId, CancellationToken cancellationToken) =>
        FromResult(await _historyService.DeleteTargetPlanAsync(RequireStudentId(), planId, cancellationToken));
}
