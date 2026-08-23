using GPAHub.Domain.Entities;

namespace GPAHub.Application.Interfaces.Services;

public interface IPremiumActivationService
{
    Task<Subscription> CreateActivePremiumAsync(Guid studentId, DateTimeOffset nowUtc, int? durationDays, CancellationToken cancellationToken = default);
}
