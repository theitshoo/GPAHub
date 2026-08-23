using GPAHub.Application.Interfaces.Repositories;
using GPAHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GPAHub.Infrastructure.Repositories;

public class SubscriptionRepository : ISubscriptionRepository
{
    private readonly Persistence.GpaHubDbContext _context;

    public SubscriptionRepository(Persistence.GpaHubDbContext context)
    {
        _context = context;
    }

    public Task<Subscription?> GetLatestForStudentAsync(Guid studentId, CancellationToken cancellationToken = default) =>
        _context.Subscriptions
            .Include(s => s.Payments)
            .Where(s => s.StudentId == studentId)
            .OrderByDescending(s => s.StartDate)
            .ThenByDescending(s => s.Id)
            .FirstOrDefaultAsync(cancellationToken);

    public async Task AddAsync(Subscription subscription, CancellationToken cancellationToken = default) =>
        await _context.Subscriptions.AddAsync(subscription, cancellationToken);

    public void Update(Subscription subscription) => _context.Subscriptions.Update(subscription);
}
