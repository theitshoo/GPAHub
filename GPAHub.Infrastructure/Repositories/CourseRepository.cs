using GPAHub.Application.Interfaces.Repositories;
using GPAHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GPAHub.Infrastructure.Repositories;

public class CourseRepository : ICourseRepository
{
    private readonly Persistence.GpaHubDbContext _context;

    public CourseRepository(Persistence.GpaHubDbContext context)
    {
        _context = context;
    }

    public Task<Course?> GetByIdForStudentAsync(Guid courseId, Guid studentId, CancellationToken cancellationToken = default) =>
        _context.Courses.FirstOrDefaultAsync(c => c.Id == courseId && c.StudentId == studentId, cancellationToken);

    public async Task<IReadOnlyList<Course>> ListByStudentAsync(Guid studentId, Guid? semesterId = null, CancellationToken cancellationToken = default)
    {
        var query = _context.Courses.AsNoTracking().Where(c => c.StudentId == studentId);

        if (semesterId.HasValue)
        {
            query = query.Where(c => c.SemesterId == semesterId.Value);
        }

        return await query.OrderBy(c => c.Name).ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Course>> ListBySemesterTrackedAsync(Guid studentId, Guid semesterId, CancellationToken cancellationToken = default) =>
        await _context.Courses
            .Where(c => c.StudentId == studentId && c.SemesterId == semesterId)
            .OrderBy(c => c.Name)
            .ToListAsync(cancellationToken);

    public async Task AddAsync(Course course, CancellationToken cancellationToken = default) =>
        await _context.Courses.AddAsync(course, cancellationToken);

    public void Remove(Course course) => _context.Courses.Remove(course);
}
