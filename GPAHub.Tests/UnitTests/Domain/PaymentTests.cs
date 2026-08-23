using GPAHub.Domain.Entities;
using GPAHub.Domain.Enums;
using GPAHub.Domain.Exceptions;

namespace GPAHub.Tests.UnitTests.Domain;

public class PaymentTests
{
    private static readonly DateTimeOffset FixedTime = new(2026, 8, 23, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void PaymentsCreatedThroughAggregate_StartPending()
    {
        var payment = CreatePendingPayment();

        Assert.Equal(PaymentStatus.Pending, payment.Status);
        Assert.Equal(49.99m, payment.Amount);
        Assert.Equal("USD", payment.Currency);
        Assert.Equal(FixedTime, payment.OccurredAtUtc);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Creation_WithMissingExternalReference_Throws(string? reference)
    {
        var subscription = CreateSubscription();

        Assert.Throws<DomainException>(
            () => subscription.AddPayment(10m, "USD", FixedTime, reference!));
    }

    [Fact]
    public void MarkCompleted_TransitionsFromPending()
    {
        var payment = CreatePendingPayment();

        payment.MarkCompleted();

        Assert.Equal(PaymentStatus.Completed, payment.Status);
    }

    [Fact]
    public void MarkFailed_TransitionsFromPending()
    {
        var payment = CreatePendingPayment();

        payment.MarkFailed();

        Assert.Equal(PaymentStatus.Failed, payment.Status);
    }

    [Fact]
    public void MarkCompleted_AfterCompleted_Throws_TerminalState()
    {
        var payment = CreatePendingPayment();
        payment.MarkCompleted();

        Assert.Throws<DomainException>(payment.MarkCompleted);
    }

    [Fact]
    public void MarkFailed_AfterCompleted_Throws_NoTransitionOutOfTerminalStates()
    {
        var payment = CreatePendingPayment();
        payment.MarkCompleted();

        Assert.Throws<DomainException>(payment.MarkFailed);
    }

    [Fact]
    public void MarkCompleted_AfterFailed_Throws()
    {
        var payment = CreatePendingPayment();
        payment.MarkFailed();

        Assert.Throws<DomainException>(payment.MarkCompleted);
    }

    private static Subscription CreateSubscription() =>
        new(Guid.NewGuid(), SubscriptionType.Free, FixedTime.AddYears(-1), null);

    private static Payment CreatePendingPayment() =>
        CreateSubscription().AddPayment(49.99m, "USD", FixedTime, "txn-001");
}
