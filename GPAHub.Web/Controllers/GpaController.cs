using GPAHub.Application.DTOs.Gpa;
using GPAHub.Application.Interfaces.Services;
using GPAHub.Web.Controllers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GPAHub.Web.Controllers;

[Route("api/gpa")]
public class GpaController : ApiControllerBase
{
    private readonly IGpaCalculationService _gpaCalculationService;

    public GpaController(IGpaCalculationService gpaCalculationService)
    {
        _gpaCalculationService = gpaCalculationService;
    }

    [AllowAnonymous]
    [HttpPost("calculate")]
    [ProducesResponseType(typeof(GpaCalculationResponseDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<GpaCalculationResponseDto>> Calculate(CalculateGpaRequestDto request, CancellationToken cancellationToken) =>
        FromResult(await _gpaCalculationService.CalculateAsync(request, cancellationToken));

    [Authorize]
    [HttpPost("calculate-for-me")]
    public async Task<ActionResult<GpaCalculationResponseDto>> CalculateForMe(CalculateGpaRequestDto request, CancellationToken cancellationToken) =>
        FromResult(await _gpaCalculationService.CalculateForStudentAsync(RequireStudentId(), request, cancellationToken));

    [Authorize]
    [HttpPost("calculate-and-save")]
    public async Task<ActionResult<GpaCalculationResponseDto>> CalculateAndSave(CalculateGpaRequestDto request, CancellationToken cancellationToken) =>
        FromResult(await _gpaCalculationService.CalculateAndSaveAsync(RequireStudentId(), request, cancellationToken));
}
