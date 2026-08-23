using GPAHub.Application.Common;
using GPAHub.Application.DTOs.Course;
using GPAHub.Application.DTOs.Semester;

namespace GPAHub.Application.Interfaces.Services;

public interface ISemesterService
{
    Task<Result<SemesterOptionDto>> CreateAsync(Guid studentId, CreateSemesterDto dto, CancellationToken cancellationToken = default);

    Task<Result<IReadOnlyList<SemesterOptionDto>>> ListByStudentAsync(Guid studentId, CancellationToken cancellationToken = default);

    Task<Result> RenameAsync(Guid studentId, Guid semesterId, UpdateSemesterDto dto, CancellationToken cancellationToken = default);

    Task<Result> DeleteAsync(Guid studentId, Guid semesterId, CancellationToken cancellationToken = default);
}
