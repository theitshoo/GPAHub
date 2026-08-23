using GPAHub.Application.DTOs.Target;
using GPAHub.Application.Interfaces.Services;
using GPAHub.Web.Controllers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GPAHub.Web.Controllers;

[Route("api/target")]
public class TargetController : ApiControllerBase
{
    private readonly ITargetGpaService _targetGpaService;

    public TargetController(ITargetGpaService targetGpaService)
    {
        _targetGpaService = targetGpaService;
    }

    [HttpPost("predict")]
    [ProducesResponseType(typeof(TargetPredictionResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<TargetPredictionResponseDto>> Predict(TargetPredictionRequestDto request, CancellationToken cancellationToken) =>
        FromResult(await _targetGpaService.PredictAsync(request, CurrentStudentId, cancellationToken));

    [Authorize]
    [HttpPost("predict-and-save")]
    public async Task<ActionResult<TargetPredictionResponseDto>> PredictAndSave(TargetPredictionRequestDto request, CancellationToken cancellationToken) =>
        FromResult(await _targetGpaService.PredictAndSaveAsync(RequireStudentId(), request, cancellationToken));
}
