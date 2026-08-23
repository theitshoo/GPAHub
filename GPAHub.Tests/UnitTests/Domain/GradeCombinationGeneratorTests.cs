using GPAHub.Domain.Constants;
using GPAHub.Domain.DomainServices;
using GPAHub.Domain.Entities;
using GPAHub.Domain.Exceptions;

namespace GPAHub.Tests.UnitTests.Domain;

public class GradeCombinationGeneratorTests
{
    private static List<GradeDefinition> CreateDefinitions() =>
    [
        new("A", 90, 100, 4.0m),
        new("B", 80, 89, 3.0m),
        new("C", 70, 79, 2.0m),
        new("F", 0, 59, 0.0m)
    ];

    [Fact]
    public void Generate_FreshmanSimpleCase_ReturnsValidCombosClosestFirst()
    {
        var upcoming = new List<UpcomingCourseInput> { new("Math", 3m), new("Art", 3m) };

        var result = GradeCombinationGenerator.Generate(
            currentQualityPoints: 0m,
            completedCreditHours: 0m,
            targetGpa: 3.5m,
            upcomingCourses: upcoming,
            availableGrades: CreateDefinitions());

        Assert.All(result.Combinations, c => Assert.True(c.ResultingGpa >= 3.5m));
        var ordered = result.Combinations.Select(c => c.ResultingGpa).ToList();
        Assert.Equal(ordered.OrderBy(g => g), ordered);
        Assert.Contains(result.Combinations, c =>
            c.ResultingGpa == 3.5m &&
            c.Assignments.Any(a => a.CourseName == "Math" && a.GradeName == "A") &&
            c.Assignments.Any(a => a.CourseName == "Art" && a.GradeName == "B"));
        Assert.Contains(result.Combinations, c => c.ResultingGpa == 4.0m);
    }

    [Fact]
    public void Generate_WithBaseline_UsesBlendedResultingGpa()
    {
        var upcoming = new List<UpcomingCourseInput> { new("X", 3m), new("Y", 3m) };

        var result = GradeCombinationGenerator.Generate(
            currentQualityPoints: 30m,
            completedCreditHours: 10m,
            targetGpa: 3.25m,
            upcomingCourses: upcoming,
            availableGrades: [new GradeDefinition("A", 90, 100, 4.0m), new GradeDefinition("B", 80, 89, 3.0m)]);

        var allA = result.Combinations.Single(c => c.Assignments.All(a => a.GradeName == "A"));
        Assert.Equal(54m / 16m, allA.ResultingGpa);
        Assert.DoesNotContain(result.Combinations, c => c.Assignments.Any(a => a.GradeName == "B"));
    }

    [Fact]
    public void Generate_ImpossibleTarget_ReturnsEmptyNotException()
    {
        var upcoming = new List<UpcomingCourseInput> { new("Math", 3m) };

        var result = GradeCombinationGenerator.Generate(0m, 0m, 99m, upcoming, CreateDefinitions());

        Assert.Empty(result.Combinations);
        Assert.False(result.SearchWasTruncated);
    }

    [Fact]
    public void Generate_RespectsMaxResultsCap()
    {
        var upcoming = Enumerable.Range(1, CombinationLimits.MaxUpcomingCourses)
            .Select(i => new UpcomingCourseInput($"C{i}", 3m))
            .ToList();

        var result = GradeCombinationGenerator.Generate(0m, 0m, 2.0m, upcoming, CreateDefinitions());

        Assert.True(result.Combinations.Count <= CombinationLimits.MaxResultsReturned);
    }

    [Fact]
    public void Generate_TooManyCourses_Throws()
    {
        var upcoming = Enumerable.Range(1, CombinationLimits.MaxUpcomingCourses + 1)
            .Select(i => new UpcomingCourseInput($"C{i}", 3m))
            .ToList();

        Assert.Throws<DomainException>(
            () => GradeCombinationGenerator.Generate(0m, 0m, 3.0m, upcoming, CreateDefinitions()));
    }

    [Fact]
    public void Generate_EmptyUpcomingCourses_Throws()
    {
        Assert.Throws<DomainException>(
            () => GradeCombinationGenerator.Generate(0m, 0m, 3.0m, [], CreateDefinitions()));
    }

    [Fact]
    public void Generate_NoAvailableGrades_Throws()
    {
        var upcoming = new List<UpcomingCourseInput> { new("Math", 3m) };

        Assert.Throws<DomainException>(
            () => GradeCombinationGenerator.Generate(0m, 0m, 3.0m, upcoming, []));
    }

    [Fact]
    public void Generate_EqualPointsDefinitions_ProduceDistinctAssignments()
    {
        var definitions = new List<GradeDefinition>
        {
            new("A", 93, 100, 4.0m),
            new("A-", 90, 92, 4.0m)
        };
        var upcoming = new List<UpcomingCourseInput> { new("Solo", 3m) };

        var result = GradeCombinationGenerator.Generate(0m, 0m, 4.0m, upcoming, definitions);

        Assert.Equal(2, result.Combinations.Count);
        Assert.All(result.Combinations, c => Assert.Equal(4.0m, c.ResultingGpa));
    }

    [Fact]
    public void Generate_UnsortedGradeInput_IsHandled()
    {
        var definitions = new List<GradeDefinition>
        {
            new("F", 0, 59, 0.0m),
            new("A", 90, 100, 4.0m),
            new("B", 70, 79, 2.5m)
        };
        var upcoming = new List<UpcomingCourseInput> { new("Solo", 3m) };

        var result = GradeCombinationGenerator.Generate(0m, 0m, 4.0m, upcoming, definitions);

        Assert.Contains(result.Combinations, c => c.ResultingGpa == 4.0m);
    }
}
