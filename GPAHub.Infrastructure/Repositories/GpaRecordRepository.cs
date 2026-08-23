using GPAHub.Application.Interfaces.Repositories;
using GPAHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GPAHub.Infrastructure.Repositories;

public class GpaRecordRepository : IGpaRecordRepository
{
    private readonly Persistence.GpaHubDbContext _context;

    public GpaRecordRepository(Persistence.GpaHubDbContext context)
    {
        _context = context;
    }

    public Task<GpaRecord?> GetByIdForStudentAsync(Guid recordId, Guid studentId, CancellationToken cancellationToken = default) =>
        _context.GpaRecords
            .Include(r => r.CourseLines)
            .FirstOrDefaultAsync(r => r.Id == recordId && r.StudentId == studentId, cancellationToken);

    public async Task<(IReadOnlyList<GpaRecord> Items, int TotalCount)> ListByStudentAsync(
        Guid studentId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = _context.GpaRecords
            .AsNoTracking()
            .Where(r => r.StudentId == studentId);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(r => r.CreatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task AddAsync(GpaRecord record, CancellationToken cancellationToken = default) =>
        await _context.GpaRecords.AddAsync(record, cancellationToken);

    public void Remove(GpaRecord record) => _context.GpaRecords.Remove(record);
}
