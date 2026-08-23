using GPAHub.Domain.Entities;
using GPAHub.Domain.Exceptions;

namespace GPAHub.Tests.UnitTests.Domain;

public class GradeScaleTests
{
    private static GradeScale CreateScale(bool enforceFullCoverage = false) =>
        new("Default Scale", studentId: null, description: "Test scale", enforceFullCoverage: enforceFullCoverage);

    [Fact]
    public void Constructor_WithValidData_CreatesEmptyScale()
    {
        var scale = CreateScale();

        Assert.NotEqual(Guid.Empty, scale.Id);
        Assert.Equal("Default Scale", scale.Name);
        Assert.Equal("Test scale", scale.Description);
        Assert.Null(scale.StudentId);
        Assert.False(scale.IsActive);
        Assert.False(scale.EnforceFullCoverage);
        Assert.Empty(scale.Definitions);
    }

    [Fact]
    public void Constructor_WithStudent_SetsOwner()
    {
        var studentId = Guid.NewGuid();
        var scale = new GradeScale("My Scale", studentId);

        Assert.Equal(studentId, scale.StudentId);
    }

    [Fact]
    public void Constructor_TrimsNameAndDescription()
    {
        var scale = new GradeScale("  Scale  ", null, "  desc  ");

        Assert.Equal("Scale", scale.Name);
        Assert.Equal("desc", scale.Description);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithMissingName_Throws(string? name)
    {
        Assert.Throws<DomainException>(() => new GradeScale(name!, null));
    }

    [Fact]
    public void AddDefinition_WithValidData_AddsDefinition()
    {
        var scale = CreateScale();

        var definition = scale.AddDefinition("A", 90, 100, 4.0m);

        Assert.Single(scale.Definitions);
        Assert.Equal(definition.Id, scale.Definitions.Single().Id);
        Assert.Equal(4.0m, definition.Points);
    }

    [Fact]
    public void AddDefinition_WithDuplicateNameDifferentCase_Throws()
    {
        var scale = CreateScale();
        scale.AddDefinition("A", 90, 100, 4.0m);

        Assert.Throws<DomainException>(() => scale.AddDefinition("a", 50, 60, 2.0m));
    }

    [Fact]
    public void AddDefinition_WhenMinGreaterThanMax_Throws()
    {
        var scale = CreateScale();

        Assert.Throws<DomainException>(() => scale.AddDefinition("B", 80, 70, 3.0m));
    }

    [Fact]
    public void AddDefinition_WithOverlappingRange_Throws()
    {
        var scale = CreateScale();
        scale.AddDefinition("A", 90, 100, 4.0m);

        Assert.Throws<DomainException>(() => scale.AddDefinition("B", 85, 95, 3.0m));
    }

    [Fact]
    public void AddDefinition_WithAdjacentRanges_IsAllowed()
    {
        var scale = CreateScale();
        scale.AddDefinition("B", 80, 89, 3.0m);
        scale.AddDefinition("A", 90, 100, 4.0m);

        Assert.Equal(2, scale.Definitions.Count);
    }

    [Fact]
    public void UpdateDefinition_WithValidData_AppliesChanges()
    {
        var scale = CreateScale();
        var definition = scale.AddDefinition("B", 80, 89, 3.0m);

        scale.UpdateDefinition(definition.Id, "B+", 87, 89, 3.3m);

        Assert.Equal("B+", definition.Name);
        Assert.Equal(87, definition.MinMark);
        Assert.Equal(89, definition.MaxMark);
        Assert.Equal(3.3m, definition.Points);
    }

    [Fact]
    public void UpdateDefinition_KeepingOwnName_IsAllowed()
    {
        var scale = CreateScale();
        var definition = scale.AddDefinition("B", 80, 89, 3.0m);
        scale.AddDefinition("A", 90, 100, 4.0m);

        scale.UpdateDefinition(definition.Id, "B", 75, 79, 3.0m);

        Assert.Equal(75, definition.MinMark);
        Assert.Equal(2, scale.Definitions.Count);
    }

    [Fact]
    public void UpdateDefinition_ToConflictingName_Throws()
    {
        var scale = CreateScale();
        var definition = scale.AddDefinition("B", 80, 89, 3.0m);
        scale.AddDefinition("A", 90, 100, 4.0m);

        Assert.Throws<DomainException>(() => scale.UpdateDefinition(definition.Id, "A", 70, 79, 3.0m));
    }

    [Fact]
    public void UpdateDefinition_ToOverlappingRange_Throws()
    {
        var scale = CreateScale();
        var definition = scale.AddDefinition("B", 80, 89, 3.0m);
        scale.AddDefinition("A", 90, 100, 4.0m);

        Assert.Throws<DomainException>(() => scale.UpdateDefinition(definition.Id, "B+", 95, 99, 3.3m));
    }

    [Fact]
    public void UpdateDefinition_WithUnknownId_Throws()
    {
        var scale = CreateScale();

        Assert.Throws<DomainException>(() => scale.UpdateDefinition(Guid.NewGuid(), "X", 1, 2, 1.0m));
    }

    [Fact]
    public void RemoveDefinition_RemovesEntry_AndNameBecomesReusable()
    {
        var scale = CreateScale();
        var definition = scale.AddDefinition("B", 80, 89, 3.0m);

        scale.RemoveDefinition(definition.Id);
        scale.AddDefinition("B", 60, 69, 2.0m);

        Assert.Single(scale.Definitions);
        Assert.Equal(60, scale.Definitions.Single().MinMark);
    }

    [Fact]
    public void RemoveDefinition_WithUnknownId_Throws()
    {
        var scale = CreateScale();

        Assert.Throws<DomainException>(() => scale.RemoveDefinition(Guid.NewGuid()));
    }

    [Fact]
    public void EnsureValid_WithDefinitions_Passes()
    {
        var scale = CreateScale();
        scale.AddDefinition("A", 90, 100, 4.0m);

        scale.EnsureValid();
    }

    [Fact]
    public void EnsureValid_WithNoDefinitions_ThrowsWithErrors()
    {
        var scale = CreateScale();

        var exception = Assert.Throws<InvalidGradeScaleException>(scale.EnsureValid);

        Assert.NotEmpty(exception.Errors);
    }

    [Fact]
    public void EnsureValid_WithoutCoverageFlag_AllowsGaps()
    {
        var scale = CreateScale(enforceFullCoverage: false);
        scale.AddDefinition("D", 60, 69, 1.0m);
        scale.AddDefinition("C", 70, 79, 2.0m);

        scale.EnsureValid();
    }

    [Fact]
    public void EnsureValid_WithCoverageFlag_AndGap_Throws()
    {
        var scale = CreateScale(enforceFullCoverage: true);
        scale.AddDefinition("D", 60, 69, 1.0m);
        scale.AddDefinition("C", 70, 79, 2.0m);

        Assert.Throws<InvalidGradeScaleException>(scale.EnsureValid);
    }

    [Fact]
    public void EnsureValid_WithCoverageFlag_AndFullSpan_Passes()
    {
        var scale = CreateScale(enforceFullCoverage: true);
        scale.AddDefinition("F", 0, 59, 0.0m);
        scale.AddDefinition("A", 60, 100, 4.0m);

        scale.EnsureValid();
    }

    [Fact]
    public void Activate_SetsIsActiveTrue()
    {
        var scale = CreateScale();

        scale.Activate();

        Assert.True(scale.IsActive);
    }

    [Fact]
    public void Deactivate_SetsIsActiveFalse()
    {
        var scale = CreateScale();
        scale.Activate();

        scale.Deactivate();

        Assert.False(scale.IsActive);
    }
}
