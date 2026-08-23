using GPAHub.Application.Common;
using GPAHub.Application.DTOs.Target;

namespace GPAHub.Application.Interfaces.Services;

public interface ITargetGpaService
{
    Task<Result<TargetPredictionResponseDto>> PredictAsync(TargetPredictionRequestDto request, CancellationToken cancellationToken = default);

    Task<Result<TargetPredictionResponseDto>> PredictAndSaveAsync(Guid studentId, TargetPredictionRequestDto request, CancellationToken cancellationToken = default);
}
