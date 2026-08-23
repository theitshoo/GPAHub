using GPAHub.Application.Common;
using GPAHub.Application.DTOs.Course;

namespace GPAHub.Application.Interfaces.Services;

public interface ICourseService
{
    Task<Result<CourseDto>> CreateAsync(Guid studentId, CourseInputDto dto, CancellationToken cancellationToken = default);

    Task<Result<CourseDto>> UpdateAsync(Guid studentId, Guid courseId, CourseInputDto dto, CancellationToken cancellationToken = default);

    Task<Result> DeleteAsync(Guid studentId, Guid courseId, CancellationToken cancellationToken = default);

    Task<Result<CourseDto>> GetByIdAsync(Guid studentId, Guid courseId, CancellationToken cancellationToken = default);

    Task<Result<IReadOnlyList<CourseDto>>> ListByStudentAsync(Guid studentId, Guid? semesterId = null, CancellationToken cancellationToken = default);
}
