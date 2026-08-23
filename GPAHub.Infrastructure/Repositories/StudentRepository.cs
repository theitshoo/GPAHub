using GPAHub.Application.Interfaces.Repositories;
using GPAHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GPAHub.Infrastructure.Repositories;

public class StudentRepository : IStudentRepository
{
    private readonly Persistence.GpaHubDbContext _context;

    public StudentRepository(Persistence.GpaHubDbContext context)
    {
        _context = context;
    }

    public Task<Student?> GetByIdAsync(Guid studentId, CancellationToken cancellationToken = default) =>
        _context.Students.FirstOrDefaultAsync(s => s.Id == studentId, cancellationToken);

    public Task<Student?> GetByEmailAsync(string email, CancellationToken cancellationToken = default) =>
        _context.Students.FirstOrDefaultAsync(
            s => s.Email == email.Trim().ToLowerInvariant(),
            cancellationToken);

    public Task<bool> ExistsByEmailAsync(string email, CancellationToken cancellationToken = default) =>
        _context.Students.AnyAsync(
            s => s.Email == email.Trim().ToLowerInvariant(),
            cancellationToken);

    public async Task AddAsync(Student student, CancellationToken cancellationToken = default) =>
        await _context.Students.AddAsync(student, cancellationToken);
}
