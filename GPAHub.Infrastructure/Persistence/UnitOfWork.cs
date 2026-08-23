using GPAHub.Application.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace GPAHub.Infrastructure.Persistence;

public class UnitOfWork : IUnitOfWork
{
    private readonly GpaHubDbContext _context;

    public UnitOfWork(GpaHubDbContext context)
    {
        _context = context;
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _context.SaveChangesAsync(cancellationToken);
}
