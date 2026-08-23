using GPAHub.Domain.Exceptions;
using GPAHub.Domain.ValueObjects;

namespace GPAHub.Tests.UnitTests.Domain;

public class MoneyTests
{
    [Fact]
    public void Constructor_WithPositiveAmount_DefaultsCurrencyToUsd()
    {
        var money = new Money(99.99m);

        Assert.Equal(99.99m, money.Amount);
        Assert.Equal("USD", money.Currency);
    }

    [Fact]
    public void Constructor_WithExplicitCurrency_NormalizesToUpperCase()
    {
        var money = new Money(250m, "egp");

        Assert.Equal("EGP", money.Currency);
    }

    [Fact]
    public void Constructor_WithZeroAmount_IsAllowed()
    {
        var money = new Money(0m);

        Assert.Equal(0m, money.Amount);
    }

    [Fact]
    public void Constructor_WithNegativeAmount_Throws()
    {
        Assert.Throws<DomainException>(() => new Money(-1m));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithMissingCurrency_Throws(string? currency)
    {
        Assert.Throws<DomainException>(() => new Money(10m, currency!));
    }

    [Theory]
    [InlineData("US")]
    [InlineData("USDD")]
    [InlineData("us1")]
    [InlineData("ÄBC")]
    public void Constructor_WithMalformedCurrency_Throws(string currency)
    {
        Assert.Throws<DomainException>(() => new Money(10m, currency));
    }

    [Fact]
    public void Equality_SameAmountAndCurrency_AreEqual()
    {
        Assert.Equal(new Money(50m, "USD"), new Money(50m, "usd"));
    }
}
