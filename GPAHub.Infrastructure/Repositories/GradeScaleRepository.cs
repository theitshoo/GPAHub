using GPAHub.Application.Interfaces.Repositories;
using GPAHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GPAHub.Infrastructure.Repositories;

public class GradeScaleRepository : IGradeScaleRepository
{
    private readonly Persistence.GpaHubDbContext _context;

    public GradeScaleRepository(Persistence.GpaHubDbContext context)
    {
        _context = context;
    }

    public Task<GradeScale?> GetByIdForStudentAsync(Guid scaleId, Guid studentId, CancellationToken cancellationToken = default) =>
        _context.GradeScales
            .Include(s => s.Definitions)
            .FirstOrDefaultAsync(s => s.Id == scaleId && s.StudentId == studentId, cancellationToken);

    public Task<GradeScale?> GetActiveForStudentAsync(Guid studentId, CancellationToken cancellationToken = default) =>
        _context.GradeScales
            .Include(s => s.Definitions)
            .FirstOrDefaultAsync(s => s.StudentId == studentId && s.IsActive, cancellationToken);

    public Task<GradeScale?> GetSystemDefaultAsync(CancellationToken cancellationToken = default) =>
        _context.GradeScales
            .Include(s => s.Definitions)
            .FirstOrDefaultAsync(s => s.StudentId == null && s.IsActive, cancellationToken);

    public async Task<IReadOnlyList<GradeScale>> ListByStudentAsync(Guid studentId, CancellationToken cancellationToken = default) =>
        await _context.GradeScales
            .Include(s => s.Definitions)
            .Where(s => s.StudentId == studentId)
            .OrderByDescending(s => s.IsActive)
            .ThenBy(s => s.Name)
            .ToListAsync(cancellationToken);

    public async Task AddAsync(GradeScale gradeScale, CancellationToken cancellationToken = default) =>
        await _context.GradeScales.AddAsync(gradeScale, cancellationToken);

    public void Remove(GradeScale gradeScale) => _context.GradeScales.Remove(gradeScale);
}
