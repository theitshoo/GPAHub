using GPAHub.Domain.Entities;

namespace GPAHub.Application.Interfaces.Repositories;

public interface IGradeScaleRepository
{
    Task<GradeScale?> GetByIdForStudentAsync(Guid scaleId, Guid studentId, CancellationToken cancellationToken = default);

    Task<GradeScale?> GetActiveForStudentAsync(Guid studentId, CancellationToken cancellationToken = default);

    Task<GradeScale?> GetSystemDefaultAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<GradeScale>> ListByStudentAsync(Guid studentId, CancellationToken cancellationToken = default);

    Task AddAsync(GradeScale gradeScale, CancellationToken cancellationToken = default);

    void Remove(GradeScale gradeScale);
}
