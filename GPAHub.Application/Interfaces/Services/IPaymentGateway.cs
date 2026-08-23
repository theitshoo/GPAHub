using GPAHub.Application.DTOs.Payments;

namespace GPAHub.Application.Interfaces.Services;

public record CheckoutSessionRequest(
    Guid StudentId,
    decimal Amount,
    string Currency,
    int? DurationDays,
    string SuccessUrl,
    string CancelUrl);

public sealed record CheckoutSessionResult(string SessionId, string CheckoutUrl);

public interface IPaymentGateway
{
    Task<CheckoutSessionResult> CreatePremiumCheckoutSessionAsync(CheckoutSessionRequest request, CancellationToken cancellationToken = default);

    bool VerifyWebhookSignature(string rawBody, string signatureHeader);
}
