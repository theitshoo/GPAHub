using GPAHub.Application.DTOs.GradeScale;
using GPAHub.Application.Interfaces.Services;
using GPAHub.Web.Controllers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GPAHub.Web.Controllers;

[Authorize]
[Route("api/grade-scales")]
public class GradeScalesController : ApiControllerBase
{
    private readonly IGradeScaleService _service;

    public GradeScalesController(IGradeScaleService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<GradeScaleDto>>> List(CancellationToken cancellationToken) =>
        FromResult(await _service.ListByStudentAsync(RequireStudentId(), cancellationToken));

    [HttpGet("{scaleId:guid}")]
    public async Task<ActionResult<GradeScaleDto>> GetById(Guid scaleId, CancellationToken cancellationToken) =>
        FromResult(await _service.GetByIdAsync(RequireStudentId(), scaleId, cancellationToken));

    [HttpPost]
    public async Task<ActionResult<GradeScaleDto>> Create(CreateGradeScaleDto dto, CancellationToken cancellationToken)
    {
        var result = await _service.CreateAsync(RequireStudentId(), dto, cancellationToken);

        return result.IsSuccess
            ? CreatedAtAction(nameof(GetById), new { scaleId = result.Value.Id }, result.Value)
            : FromError(result.Error!);
    }

    [HttpPut("{scaleId:guid}")]
    public async Task<ActionResult<GradeScaleDto>> Update(Guid scaleId, UpdateGradeScaleDto dto, CancellationToken cancellationToken) =>
        FromResult(await _service.UpdateAsync(RequireStudentId(), scaleId, dto, cancellationToken));

    [HttpDelete("{scaleId:guid}")]
    public async Task<IActionResult> Delete(Guid scaleId, CancellationToken cancellationToken) =>
        FromResult(await _service.DeleteAsync(RequireStudentId(), scaleId, cancellationToken));

    [HttpPost("{scaleId:guid}/definitions")]
    public async Task<ActionResult<GradeScaleDto>> AddDefinition(Guid scaleId, SaveGradeDefinitionDto dto, CancellationToken cancellationToken) =>
        FromResult(await _service.AddDefinitionAsync(RequireStudentId(), scaleId, dto, cancellationToken));

    [HttpPut("{scaleId:guid}/definitions/{definitionId:guid}")]
    public async Task<ActionResult<GradeScaleDto>> UpdateDefinition(Guid scaleId, Guid definitionId, SaveGradeDefinitionDto dto, CancellationToken cancellationToken) =>
        FromResult(await _service.UpdateDefinitionAsync(RequireStudentId(), scaleId, definitionId, dto, cancellationToken));

    [HttpDelete("{scaleId:guid}/definitions/{definitionId:guid}")]
    public async Task<ActionResult<GradeScaleDto>> RemoveDefinition(Guid scaleId, Guid definitionId, CancellationToken cancellationToken) =>
        FromResult(await _service.RemoveDefinitionAsync(RequireStudentId(), scaleId, definitionId, cancellationToken));

    [HttpPost("{scaleId:guid}/activate")]
    public async Task<ActionResult<GradeScaleDto>> Activate(Guid scaleId, CancellationToken cancellationToken) =>
        FromResult(await _service.SetActiveAsync(RequireStudentId(), scaleId, isActive: true, cancellationToken));

    [HttpPost("{scaleId:guid}/deactivate")]
    public async Task<ActionResult<GradeScaleDto>> Deactivate(Guid scaleId, CancellationToken cancellationToken) =>
        FromResult(await _service.SetActiveAsync(RequireStudentId(), scaleId, isActive: false, cancellationToken));

    [AllowAnonymous]
    [HttpGet("system-default")]
    public async Task<ActionResult<GradeScaleDto>> GetSystemDefault(CancellationToken cancellationToken) =>
        FromResult(await _service.GetSystemDefaultAsync(cancellationToken));
}
