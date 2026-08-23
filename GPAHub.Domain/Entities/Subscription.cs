using GPAHub.Domain.Enums;
using GPAHub.Domain.Exceptions;

namespace GPAHub.Domain.Entities;

public sealed class Subscription
{
    private readonly List<Payment> _payments = [];

    public Guid Id { get; private set; }

    public Guid StudentId { get; private set; }

    public SubscriptionType Type { get; private set; }

    public DateTimeOffset StartDate { get; private set; }

    public DateTimeOffset? EndDate { get; private set; }

    public SubscriptionStatus Status { get; private set; }

    public IReadOnlyList<Payment> Payments => _payments.AsReadOnly();

    private Subscription()
    {
    }

    public Subscription(Guid studentId, SubscriptionType type, DateTimeOffset startDateUtc, DateTimeOffset? endDateUtc)
    {
        if (studentId == Guid.Empty)
        {
            throw new DomainException("Student id is required.");
        }

        if (endDateUtc.HasValue && endDateUtc.Value < startDateUtc)
        {
            throw new DomainException("Subscription end date cannot be before start date.");
        }

        Id = Guid.NewGuid();
        StudentId = studentId;
        Type = type;
        StartDate = startDateUtc;
        EndDate = endDateUtc;
        Status = SubscriptionStatus.Active;
    }

    public bool IsActiveAsOf(DateTimeOffset utcNow) =>
        Status == SubscriptionStatus.Active &&
        utcNow >= StartDate &&
        (!EndDate.HasValue || utcNow <= EndDate.Value);

    public void Expire() => Status = SubscriptionStatus.Expired;

    public void Activate(DateTimeOffset? endDateUtc)
    {
        if (endDateUtc.HasValue && endDateUtc.Value <= DateTimeOffset.UtcNow)
        {
            throw new DomainException("Subscription end date must be in the future.");
        }

        EndDate = endDateUtc ?? EndDate;
        Status = SubscriptionStatus.Active;
    }

    public Payment AddPayment(decimal amount, string currency, DateTimeOffset occurredAtUtc, string externalReference)
    {
        var payment = new Payment(Id, amount, currency, occurredAtUtc, externalReference);

        _payments.Add(payment);

        return payment;
    }
}
