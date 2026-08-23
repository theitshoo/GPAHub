using GPAHub.Domain.Entities;

namespace GPAHub.Application.Interfaces.Repositories;

public interface ISemesterRepository
{
    Task<Semester?> GetByIdForStudentAsync(Guid semesterId, Guid studentId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Semester>> ListByStudentAsync(Guid studentId, CancellationToken cancellationToken = default);

    Task AddAsync(Semester semester, CancellationToken cancellationToken = default);

    void Remove(Semester semester);
}
