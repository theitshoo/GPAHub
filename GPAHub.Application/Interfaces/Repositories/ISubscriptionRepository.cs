using GPAHub.Domain.Entities;

namespace GPAHub.Application.Interfaces.Repositories;

public interface ISubscriptionRepository
{
    Task<Subscription?> GetLatestForStudentAsync(Guid studentId, CancellationToken cancellationToken = default);

    Task AddAsync(Subscription subscription, CancellationToken cancellationToken = default);

    void Update(Subscription subscription);
}
