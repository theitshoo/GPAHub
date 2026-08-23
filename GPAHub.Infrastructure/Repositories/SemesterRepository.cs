using GPAHub.Application.Interfaces.Repositories;
using GPAHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GPAHub.Infrastructure.Repositories;

public class SemesterRepository : ISemesterRepository
{
    private readonly Persistence.GpaHubDbContext _context;

    public SemesterRepository(Persistence.GpaHubDbContext context)
    {
        _context = context;
    }

    public Task<Semester?> GetByIdForStudentAsync(Guid semesterId, Guid studentId, CancellationToken cancellationToken = default) =>
        _context.Semesters.FirstOrDefaultAsync(s => s.Id == semesterId && s.StudentId == studentId, cancellationToken);

    public async Task<IReadOnlyList<Semester>> ListByStudentAsync(Guid studentId, CancellationToken cancellationToken = default) =>
        await _context.Semesters
            .AsNoTracking()
            .Where(s => s.StudentId == studentId)
            .OrderBy(s => s.Name)
            .ToListAsync(cancellationToken);

    public async Task AddAsync(Semester semester, CancellationToken cancellationToken = default) =>
        await _context.Semesters.AddAsync(semester, cancellationToken);

    public void Remove(Semester semester) => _context.Semesters.Remove(semester);
}
