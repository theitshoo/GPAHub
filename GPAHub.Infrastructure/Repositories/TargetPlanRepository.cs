using GPAHub.Application.Interfaces.Repositories;
using GPAHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GPAHub.Infrastructure.Repositories;

public class TargetPlanRepository : ITargetPlanRepository
{
    private readonly Persistence.GpaHubDbContext _context;

    public TargetPlanRepository(Persistence.GpaHubDbContext context)
    {
        _context = context;
    }

    public Task<TargetPlan?> GetByIdForStudentAsync(Guid planId, Guid studentId, CancellationToken cancellationToken = default) =>
        _context.TargetPlans
            .Include(p => p.UpcomingCourses)
            .FirstOrDefaultAsync(p => p.Id == planId && p.StudentId == studentId, cancellationToken);

    public async Task<(IReadOnlyList<TargetPlan> Items, int TotalCount)> ListByStudentAsync(
        Guid studentId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = _context.TargetPlans
            .AsNoTracking()
            .Where(p => p.StudentId == studentId);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(p => p.CreatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task AddAsync(TargetPlan plan, CancellationToken cancellationToken = default) =>
        await _context.TargetPlans.AddAsync(plan, cancellationToken);

    public void Remove(TargetPlan plan) => _context.TargetPlans.Remove(plan);
}
