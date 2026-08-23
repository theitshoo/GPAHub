using GPAHub.Domain.Entities;
using GPAHub.Domain.Enums;
using GPAHub.Domain.Exceptions;

namespace GPAHub.Tests.UnitTests.Domain;

public class SubscriptionTests
{
    private static readonly DateTimeOffset Start = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset End = new(2027, 1, 1, 0, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset DuringPeriod = new(2026, 6, 15, 0, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset AfterEnd = new(2027, 6, 15, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Constructor_FreeLifetime_IsActiveAtAnyTime()
    {
        var subscription = new Subscription(Guid.NewGuid(), SubscriptionType.Free, Start, null);

        Assert.Equal(SubscriptionType.Free, subscription.Type);
        Assert.True(subscription.IsActiveAsOf(AfterEnd));
    }

    [Fact]
    public void IsActiveAsOf_TrueWithinPremiumPeriod()
    {
        var subscription = new Subscription(Guid.NewGuid(), SubscriptionType.Premium, Start, End);

        Assert.True(subscription.IsActiveAsOf(DuringPeriod));
    }

    [Fact]
    public void IsActiveAsOf_FalseAfterEndDate()
    {
        var subscription = new Subscription(Guid.NewGuid(), SubscriptionType.Premium, Start, End);

        Assert.False(subscription.IsActiveAsOf(AfterEnd));
    }

    [Fact]
    public void IsActiveAsOf_AtExactEndDate_IsActive_InclusiveBoundary()
    {
        var subscription = new Subscription(Guid.NewGuid(), SubscriptionType.Premium, Start, End);

        Assert.True(subscription.IsActiveAsOf(End));
    }

    [Fact]
    public void Expire_DeactivatesEvenWithValidDates()
    {
        var subscription = new Subscription(Guid.NewGuid(), SubscriptionType.Premium, Start, End);
        Assert.True(subscription.IsActiveAsOf(DuringPeriod));

        subscription.Expire();

        Assert.Equal(SubscriptionStatus.Expired, subscription.Status);
        Assert.False(subscription.IsActiveAsOf(DuringPeriod));
    }

    [Fact]
    public void Activate_ReactivatesWithNewEndDate()
    {
        var subscription = new Subscription(Guid.NewGuid(), SubscriptionType.Premium, Start, End);
        subscription.Expire();

        subscription.Activate(new DateTimeOffset(2028, 1, 1, 0, 0, 0, TimeSpan.Zero));

        Assert.Equal(SubscriptionStatus.Active, subscription.Status);
        Assert.True(subscription.IsActiveAsOf(new DateTimeOffset(2027, 6, 15, 0, 0, 0, TimeSpan.Zero)));
    }

    [Fact]
    public void Activate_WithEndDateInPast_Throws()
    {
        var subscription = new Subscription(Guid.NewGuid(), SubscriptionType.Premium, Start, null);

        Assert.Throws<DomainException>(() => subscription.Activate(Start.AddDays(-1)));
    }

    [Fact]
    public void AddPayment_LinksWithSubscription()
    {
        var subscription = new Subscription(Guid.NewGuid(), SubscriptionType.Free, Start, null);

        var payment = subscription.AddPayment(amount: 9.99m, currency: "USD", occurredAtUtc: DuringPeriod, externalReference: "ext-123");

        Assert.Single(subscription.Payments);
        Assert.Equal(subscription.Id, payment.SubscriptionId);
        Assert.Equal(PaymentStatus.Pending, payment.Status);
        Assert.Equal("ext-123", payment.ExternalReference);
    }
}
