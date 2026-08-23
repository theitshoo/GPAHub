using GPAHub.Application.Common;
using GPAHub.Application.DTOs.Payments;
using GPAHub.Application.DTOs.Subscription;

namespace GPAHub.Application.Interfaces.Services;

public interface IPaymentService
{
    Task<Result<BeginUpgradeResponseDto>> BeginPremiumUpgradeAsync(Guid studentId, UpgradeToPremiumDto dto, CancellationToken cancellationToken = default);

    Task<Result> ApplyPremiumPaymentSucceededAsync(string sessionId, Guid studentId, int? durationDays, CancellationToken cancellationToken = default);

    bool IsWebhookSignatureValid(string rawBody, string signatureHeader);
}
