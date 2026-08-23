using GPAHub.Application.Common;
using GPAHub.Application.DTOs.Student;

namespace GPAHub.Application.Interfaces.Services;

public interface IAcademicRecordService
{
    Task<Result<StudentProfileDto>> GetProfileAsync(Guid studentId, CancellationToken cancellationToken = default);

    Task<Result<StudentProfileDto>> UpdateProfileAsync(Guid studentId, UpdateProfileDto dto, CancellationToken cancellationToken = default);

    Task<Result<AcademicBaselineDto>> GetBaselineAsync(Guid studentId, CancellationToken cancellationToken = default);

    Task<Result<AcademicBaselineDto>> UpdateBaselineAsync(Guid studentId, UpdateBaselineDto dto, CancellationToken cancellationToken = default);

    Task<Result> ClearBaselineAsync(Guid studentId, CancellationToken cancellationToken = default);
}
