using GPAHub.Application.Common;
using GPAHub.Application.DTOs.GradeScale;

namespace GPAHub.Application.Interfaces.Services;

public interface IGradeScaleService
{
    Task<Result<GradeScaleDto>> CreateAsync(Guid studentId, CreateGradeScaleDto dto, CancellationToken cancellationToken = default);

    Task<Result<GradeScaleDto>> UpdateAsync(Guid studentId, Guid scaleId, UpdateGradeScaleDto dto, CancellationToken cancellationToken = default);

    Task<Result> DeleteAsync(Guid studentId, Guid scaleId, CancellationToken cancellationToken = default);

    Task<Result<GradeScaleDto>> AddDefinitionAsync(Guid studentId, Guid scaleId, SaveGradeDefinitionDto dto, CancellationToken cancellationToken = default);

    Task<Result<GradeScaleDto>> UpdateDefinitionAsync(Guid studentId, Guid scaleId, Guid definitionId, SaveGradeDefinitionDto dto, CancellationToken cancellationToken = default);

    Task<Result<GradeScaleDto>> RemoveDefinitionAsync(Guid studentId, Guid scaleId, Guid definitionId, CancellationToken cancellationToken = default);

    Task<Result<GradeScaleDto>> SetActiveAsync(Guid studentId, Guid scaleId, bool isActive, CancellationToken cancellationToken = default);

    Task<Result<GradeScaleDto>> GetByIdAsync(Guid studentId, Guid scaleId, CancellationToken cancellationToken = default);

    Task<Result<IReadOnlyList<GradeScaleDto>>> ListByStudentAsync(Guid studentId, CancellationToken cancellationToken = default);

    Task<Result<GradeScaleDto>> GetSystemDefaultAsync(CancellationToken cancellationToken = default);
}
