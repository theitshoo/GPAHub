using GPAHub.Domain.Exceptions;
using GPAHub.Domain.ValueObjects;

namespace GPAHub.Tests.UnitTests.Domain;

public class CreditHoursTests
{
    [Theory]
    [InlineData(1)]
    [InlineData(3)]
    [InlineData(6)]
    public void Constructor_WithPositiveWholeHours_CreatesInstance(int hours)
    {
        var creditHours = new CreditHours(hours);

        Assert.Equal(hours, creditHours.Value);
    }

    [Fact]
    public void Constructor_WithFractionalHours_CreatesInstance()
    {
        var creditHours = new CreditHours(1.5m);

        Assert.Equal(1.5m, creditHours.Value);
    }

    [Fact]
    public void Constructor_WithZero_Throws()
    {
        Assert.Throws<DomainException>(() => new CreditHours(0m));
    }

    [Fact]
    public void Constructor_WithNegative_Throws()
    {
        Assert.Throws<DomainException>(() => new CreditHours(-2m));
    }

    [Fact]
    public void Equality_SameValue_AreEqual()
    {
        Assert.Equal(new CreditHours(3m), new CreditHours(3m));
    }
}
