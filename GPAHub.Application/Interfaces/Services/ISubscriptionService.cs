using GPAHub.Application.Common;
using GPAHub.Application.DTOs.Subscription;

namespace GPAHub.Application.Interfaces.Services;

public interface ISubscriptionService
{
    Task<bool> IsPremiumAsync(Guid studentId, CancellationToken cancellationToken = default);

    Task<SubscriptionDto> GetCurrentAsync(Guid studentId, CancellationToken cancellationToken = default);

    Task<Result<SubscriptionDto>> UpgradeToPremiumAsync(Guid studentId, UpgradeToPremiumDto dto, CancellationToken cancellationToken = default);

    Task<Result> CancelAsync(Guid studentId, CancellationToken cancellationToken = default);
}
