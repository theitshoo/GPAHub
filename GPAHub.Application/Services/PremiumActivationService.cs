using GPAHub.Application.Interfaces.Repositories;
using GPAHub.Domain.Entities;
using GPAHub.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace GPAHub.Application.Services;

public class PremiumActivationService : Interfaces.Services.IPremiumActivationService
{
    private readonly ISubscriptionRepository _subscriptionRepository;
    private readonly ILogger<PremiumActivationService> _logger;

    public PremiumActivationService(ISubscriptionRepository subscriptionRepository, ILogger<PremiumActivationService> logger)
    {
        _subscriptionRepository = subscriptionRepository;
        _logger = logger;
    }

    public async Task<Subscription> CreateActivePremiumAsync(Guid studentId, DateTimeOffset nowUtc, int? durationDays, CancellationToken cancellationToken = default)
    {
        var current = await _subscriptionRepository.GetLatestForStudentAsync(studentId, cancellationToken);

        if (current is not null)
        {
            current.Expire();
            _subscriptionRepository.Update(current);
        }

        var premium = new Subscription(
            studentId,
            SubscriptionType.Premium,
            nowUtc,
            durationDays.HasValue ? nowUtc.AddDays(durationDays.Value) : null);

        await _subscriptionRepository.AddAsync(premium, cancellationToken);

        _logger.LogInformation("Premium subscription activated for student {StudentId} (expires {ExpiresAtUtc})",
            studentId, premium.EndDate?.ToString("u") ?? "never");

        return premium;
    }
}

