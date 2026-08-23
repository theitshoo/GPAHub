using GPAHub.Domain.Entities;
using GPAHub.Domain.Exceptions;

namespace GPAHub.Tests.UnitTests.Domain;

public class GradeScaleConversionTests
{
    private static GradeScale CreateScale()
    {
        var scale = new GradeScale("Standard", null);
        scale.AddDefinition("F", 0, 59, 0.0m);
        scale.AddDefinition("C", 60, 69, 2.0m);
        scale.AddDefinition("B", 70, 79, 3.0m);
        scale.AddDefinition("A", 80, 100, 4.0m);
        return scale;
    }

    [Theory]
    [InlineData(0, "F")]
    [InlineData(59, "F")]
    [InlineData(60, "C")]
    [InlineData(75, "B")]
    [InlineData(80, "A")]
    [InlineData(100, "A")]
    public void FindDefinitionForMark_ResolvesInclusiveBoundaries(int mark, string expectedGrade)
    {
        var definition = CreateScale().FindDefinitionForMark(mark);

        Assert.Equal(expectedGrade, definition!.Name);
    }

    [Fact]
    public void FindDefinitionForMark_ReturnsNullInsideGap_WhenCoverageDisabled()
    {
        var scale = new GradeScale("Sparse", null);
        scale.AddDefinition("C", 60, 69, 2.0m);
        scale.AddDefinition("A", 80, 100, 4.0m);

        var definition = scale.FindDefinitionForMark(72);

        Assert.Null(definition);
    }

    [Fact]
    public void FindDefinitionForMark_ReturnsNull_WhenNoDefinitions()
    {
        var scale = new GradeScale("Empty", null);

        Assert.Null(scale.FindDefinitionForMark(50));
    }

    [Theory]
    [InlineData("b")]
    [InlineData("B")]
    [InlineData(" b ")]
    public void FindDefinitionForGradeName_IsCaseInsensitiveAndTrims(string lookup)
    {
        var definition = CreateScale().FindDefinitionForGradeName(lookup);

        Assert.Equal("B", definition!.Name);
    }

    [Fact]
    public void FindDefinitionForGradeName_ReturnsNull_ForUnknownGrade()
    {
        Assert.Null(CreateScale().FindDefinitionForGradeName("Z"));
    }

    [Fact]
    public void GetMaxGpaPoints_ReturnsHighestDefinitionPoints()
    {
        Assert.Equal(4.0m, CreateScale().GetMaxGpaPoints());
    }

    [Fact]
    public void GetMaxGpaPoints_WithNoDefinitions_Throws()
    {
        var scale = new GradeScale("Empty", null);

        Assert.Throws<DomainException>(() => scale.GetMaxGpaPoints());
    }
}
