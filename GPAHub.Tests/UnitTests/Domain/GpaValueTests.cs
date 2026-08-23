using GPAHub.Domain.Constants;
using GPAHub.Domain.Exceptions;
using GPAHub.Domain.ValueObjects;

namespace GPAHub.Tests.UnitTests.Domain;

public class GpaValueTests
{
    [Fact]
    public void Constructor_WithZero_IsAllowed()
    {
        var gpa = new GpaValue(0m);

        Assert.Equal(0m, gpa.Value);
    }

    [Fact]
    public void Constructor_WithPositiveValue_CreatesInstance()
    {
        var gpa = new GpaValue(3.75m);

        Assert.Equal(3.75m, gpa.Value);
    }

    [Theory]
    [InlineData(-0.01)]
    [InlineData(-4)]
    public void Constructor_WithNegative_Throws(double negativeValue)
    {
        Assert.Throws<DomainException>(() => new GpaValue((decimal)negativeValue));
    }

    [Fact]
    public void Rounded_RoundsHalfAwayFromZero_ToTwoDecimalPlaces()
    {
        var gpa = new GpaValue(3.455m);

        Assert.Equal(3.46m, gpa.Rounded);
    }

    [Fact]
    public void Rounded_PreservesExactTwoDecimalValue()
    {
        var gpa = new GpaValue(2.5m);

        Assert.Equal(2.50m, gpa.Rounded);
    }

    [Fact]
    public void DisplayDecimalPlaces_MatchesConstant()
    {
        Assert.Equal(GpaConstants.DisplayDecimalPlaces, GpaValue.DisplayDecimalPlaces);
    }

    [Fact]
    public void Equality_SameValue_AreEqual()
    {
        Assert.Equal(new GpaValue(3.5m), new GpaValue(3.5m));
    }
}
