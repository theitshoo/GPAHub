using GPAHub.Domain.Entities;
using GPAHub.Domain.Exceptions;

namespace GPAHub.Tests.UnitTests.Domain;

public class GradeDefinitionTests
{
    [Fact]
    public void Constructor_WithValidData_CreatesInstance()
    {
        var definition = new GradeDefinition("A", 90, 100, 4.0m);

        Assert.Equal("A", definition.Name);
        Assert.Equal(90, definition.MinMark);
        Assert.Equal(100, definition.MaxMark);
        Assert.Equal(4.0m, definition.Points);
    }

    [Fact]
    public void Constructor_TrimsName()
    {
        var definition = new GradeDefinition("  A+  ", 93, 100, 4.0m);

        Assert.Equal("A+", definition.Name);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithMissingName_Throws(string? name)
    {
        Assert.Throws<DomainException>(() => new GradeDefinition(name!, 50, 60, 3.0m));
    }

    [Fact]
    public void Constructor_WhenMinGreaterThanMax_Throws()
    {
        Assert.Throws<DomainException>(() => new GradeDefinition("B", 80, 70, 3.0m));
    }

    [Theory]
    [InlineData(-1, 50)]
    [InlineData(101, 105)]
    public void Constructor_WithMarkOutsideAbsoluteRange_Throws(int min, int max)
    {
        Assert.Throws<DomainException>(() => new GradeDefinition("B", min, max, 3.0m));
    }

    [Theory]
    [InlineData(-0.5)]
    [InlineData(-1)]
    public void Constructor_WithNegativePoints_Throws(double negativePoints)
    {
        Assert.Throws<DomainException>(() => new GradeDefinition("B", 70, 80, (decimal)negativePoints));
    }

    [Fact]
    public void Constructor_WithZeroPoints_IsAllowed()
    {
        var definition = new GradeDefinition("F", 0, 59, 0m);

        Assert.Equal(0m, definition.Points);
    }
}
