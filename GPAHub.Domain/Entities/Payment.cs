using GPAHub.Domain.Enums;
using GPAHub.Domain.Exceptions;

namespace GPAHub.Domain.Entities;

public sealed class Payment
{
    public Guid Id { get; private set; }

    public Guid SubscriptionId { get; private set; }

    public decimal Amount { get; private set; }

    public string Currency { get; private set; }

    public PaymentStatus Status { get; private set; }

    public string ExternalReference { get; private set; }

    public DateTimeOffset OccurredAtUtc { get; private set; }

    private Payment()
    {
        Currency = string.Empty;
        ExternalReference = string.Empty;
    }

    internal Payment(Guid subscriptionId, decimal amount, string currency, DateTimeOffset occurredAtUtc, string externalReference)
    {
        var money = new ValueObjects.Money(amount, currency);

        if (string.IsNullOrWhiteSpace(externalReference))
        {
            throw new DomainException("External payment reference is required.");
        }

        Id = Guid.NewGuid();
        SubscriptionId = subscriptionId;
        Amount = money.Amount;
        Currency = money.Currency;
        Status = Enums.PaymentStatus.Pending;
        ExternalReference = externalReference.Trim();
        OccurredAtUtc = occurredAtUtc;
    }

    public void MarkCompleted()
    {
        EnsureTransitionAllowed(nameof(MarkCompleted));

        Status = Enums.PaymentStatus.Completed;
    }

    public void MarkFailed()
    {
        EnsureTransitionAllowed(nameof(MarkFailed));

        Status = Enums.PaymentStatus.Failed;
    }

    private void EnsureTransitionAllowed(string targetOperation)
    {
        if (Status == Enums.PaymentStatus.Pending)
        {
            return;
        }

        throw new DomainException(
            $"Cannot apply '{targetOperation}' to a payment already in status '{Status}'.");
    }
}
