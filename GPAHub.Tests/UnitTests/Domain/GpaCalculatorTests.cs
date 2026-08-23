using GPAHub.Domain.DomainServices;
using GPAHub.Domain.Exceptions;

namespace GPAHub.Tests.UnitTests.Domain;

public class GpaCalculatorTests
{
    [Fact]
    public void CalculateSemester_WeightedAverage_ExactExample()
    {
        var courses = new List<SemesterCourseInput>
        {
            new(CreditHours: 3m, GpaPoints: 4.0m),
            new(CreditHours: 3m, GpaPoints: 3.0m),
            new(CreditHours: 3m, GpaPoints: 2.0m)
        };

        var result = GpaCalculator.CalculateSemester(courses);

        Assert.Equal(9m, result.TotalCreditHours);
        Assert.Equal(27m, result.TotalQualityPoints);
        Assert.Equal(3.0m, result.SemesterGpa);
    }

    [Fact]
    public void CalculateSemester_DifferentWeights_ProduceCorrectWeights()
    {
        var courses = new List<SemesterCourseInput>
        {
            new(CreditHours: 4m, GpaPoints: 4.0m),
            new(CreditHours: 2m, GpaPoints: 1.0m)
        };

        var result = GpaCalculator.CalculateSemester(courses);

        Assert.Equal(18m, result.TotalQualityPoints);
        Assert.Equal(3.0m, result.SemesterGpa);
    }

    [Fact]
    public void CalculateSemester_FractionalCreditHours_Supported()
    {
        var result = GpaCalculator.CalculateSemester([new SemesterCourseInput(1.5m, 4.0m)]);

        Assert.Equal(6m, result.TotalQualityPoints);
        Assert.Equal(4.0m, result.SemesterGpa);
    }

    [Fact]
    public void CalculateSemester_ZeroHourEntry_ContributesNothing()
    {
        var courses = new List<SemesterCourseInput>
        {
            new(CreditHours: 3m, GpaPoints: 4.0m),
            new(CreditHours: 0m, GpaPoints: 4.0m)
        };

        var result = GpaCalculator.CalculateSemester(courses);

        Assert.Equal(3m, result.TotalCreditHours);
        Assert.Equal(12m, result.TotalQualityPoints);
        Assert.Equal(4.0m, result.SemesterGpa);
    }

    [Fact]
    public void CalculateSemester_EmptyList_Throws()
    {
        Assert.Throws<DomainException>(() => GpaCalculator.CalculateSemester([]));
    }

    [Fact]
    public void CalculateSemester_AllZeroHours_Throws()
    {
        Assert.Throws<DomainException>(
            () => GpaCalculator.CalculateSemester([new SemesterCourseInput(0m, 3.0m)]));
    }

    [Fact]
    public void CalculateSemester_NegativePoints_Throws()
    {
        Assert.Throws<DomainException>(
            () => GpaCalculator.CalculateSemester([new SemesterCourseInput(3m, -1m)]));
    }

    [Fact]
    public void CalculateCumulative_BlendsPreviousAndCurrent()
    {
        var result = GpaCalculator.CalculateCumulative(
            previousGpa: 3.2m,
            previousCompletedCreditHours: 60m,
            currentSemesterQualityPoints: 27m,
            currentSemesterCreditHours: 9m);

        Assert.Equal(219m, result.TotalQualityPoints);
        Assert.Equal(69m, result.TotalCreditHours);
        Assert.Equal(3.1739130434782608695652173913m, result.CumulativeGpa);
        Assert.Equal(3.17m, Math.Round(result.CumulativeGpa, 2, MidpointRounding.AwayFromZero));
    }

    [Fact]
    public void CalculateCumulative_FreshmanWithNoHistory_EqualsSemesterPerformance()
    {
        var result = GpaCalculator.CalculateCumulative(
            previousGpa: 0m,
            previousCompletedCreditHours: 0m,
            currentSemesterQualityPoints: 36m,
            currentSemesterCreditHours: 9m);

        Assert.Equal(4.0m, result.CumulativeGpa);
    }

    [Fact]
    public void CalculateCumulative_NoCreditsAtAll_Throws()
    {
        Assert.Throws<DomainException>(
            () => GpaCalculator.CalculateCumulative(0m, 0m, 0m, 0m));
    }

    [Fact]
    public void CalculateCumulative_NegativePreviousGpa_Throws()
    {
        Assert.Throws<DomainException>(() => GpaCalculator.CalculateCumulative(-1m, 30m, 12m, 3m));
    }

    [Fact]
    public void CalculateCumulative_NegativePreviousHours_Throws()
    {
        Assert.Throws<DomainException>(() => GpaCalculator.CalculateCumulative(3m, -30m, 12m, 3m));
    }
}
