using GPAHub.Domain.Entities;

namespace GPAHub.Application.Interfaces.Repositories;

public interface IGpaRecordRepository
{
    Task<GpaRecord?> GetByIdForStudentAsync(Guid recordId, Guid studentId, CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<GpaRecord> Items, int TotalCount)> ListByStudentAsync(
        Guid studentId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task AddAsync(GpaRecord record, CancellationToken cancellationToken = default);

    void Remove(GpaRecord record);
}
