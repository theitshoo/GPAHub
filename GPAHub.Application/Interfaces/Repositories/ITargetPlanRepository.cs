using GPAHub.Domain.Entities;

namespace GPAHub.Application.Interfaces.Repositories;

public interface ITargetPlanRepository
{
    Task<TargetPlan?> GetByIdForStudentAsync(Guid planId, Guid studentId, CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<TargetPlan> Items, int TotalCount)> ListByStudentAsync(
        Guid studentId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task AddAsync(TargetPlan plan, CancellationToken cancellationToken = default);

    void Remove(TargetPlan plan);
}
