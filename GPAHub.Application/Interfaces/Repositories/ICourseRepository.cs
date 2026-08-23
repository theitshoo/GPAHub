using GPAHub.Domain.Entities;

namespace GPAHub.Application.Interfaces.Repositories;

public interface ICourseRepository
{
    Task<Course?> GetByIdForStudentAsync(Guid courseId, Guid studentId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Course>> ListByStudentAsync(Guid studentId, Guid? semesterId = null, CancellationToken cancellationToken = default);

    Task AddAsync(Course course, CancellationToken cancellationToken = default);

    void Remove(Course course);
}
