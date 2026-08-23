using GPAHub.Domain.Enums;

namespace GPAHub.Application.DTOs.Subscription;

public sealed record SubscriptionDto(
    Guid? Id,
    SubscriptionType Type,
    SubscriptionStatus Status,
    DateTimeOffset StartDate,
    DateTimeOffset? EndDate,
    bool IsActive);

public sealed record PaymentDto(
    Guid Id,
    decimal Amount,
    string Currency,
    PaymentStatus Status,
    string ExternalReference,
    DateTimeOffset OccurredAtUtc);

public sealed record UpgradeToPremiumDto(
    decimal Amount,
    string Currency,
    string ExternalReference,
    int? DurationDays = 365);
