using GPAHub.Application.Common;
using GPAHub.Application.DTOs.Gpa;

namespace GPAHub.Application.Interfaces.Services;

public interface IGpaCalculationService
{
    Task<Result<GpaCalculationResponseDto>> CalculateAsync(CalculateGpaRequestDto request, CancellationToken cancellationToken = default);

    Task<Result<GpaCalculationResponseDto>> CalculateForStudentAsync(Guid studentId, CalculateGpaRequestDto request, CancellationToken cancellationToken = default);

    Task<Result<GpaCalculationResponseDto>> CalculateAndSaveAsync(Guid studentId, CalculateGpaRequestDto request, CancellationToken cancellationToken = default);
}
