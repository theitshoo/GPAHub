using GPAHub.Domain.DomainServices;
using GPAHub.Domain.Exceptions;

namespace GPAHub.Tests.UnitTests.Domain;

public class TargetGpaCalculatorTests
{
    [Fact]
    public void Predict_AchievableTarget_ComputesAllOutputs()
    {
        var upcoming = new List<UpcomingCourseInput> { new("Course A", 15m), new("Course B", 15m) };

        var result = TargetGpaCalculator.Predict(
            currentGpa: 3.2m,
            completedCreditHours: 60m,
            targetGpa: 3.4m,
            upcomingCourses: upcoming,
            maxScaleGpa: 4.0m);

        Assert.Equal(192m, result.CurrentQualityPoints);
        Assert.Equal(30m, result.TotalFutureCreditHours);
        Assert.Equal(90m, result.TotalCreditHoursAfterCompletion);
        Assert.Equal(306m, result.RequiredTotalQualityPoints);
        Assert.Equal(114m, result.RequiredFutureQualityPoints);
        Assert.Equal(3.8m, result.RequiredAverageGpa);
        Assert.True(result.IsAchievable);
        Assert.Equal(3.4666666666666666666666666667m, result.MaxReachableGpa);
    }

    [Fact]
    public void Predict_ImpossibleTarget_ReportsInfeasibilityWithMaxReachable()
    {
        var upcoming = new List<UpcomingCourseInput> { new("Course A", 30m) };

        var result = TargetGpaCalculator.Predict(
            currentGpa: 3.2m,
            completedCreditHours: 60m,
            targetGpa: 3.5m,
            upcomingCourses: upcoming,
            maxScaleGpa: 4.0m);

        Assert.Equal(4.1m, result.RequiredAverageGpa);
        Assert.False(result.IsAchievable);
        Assert.Equal((192m + 120m) / 90m, result.MaxReachableGpa);
    }

    [Fact]
    public void Predict_RequiredExactlyEqualsMax_IsAchievable_InclusiveBoundary()
    {
        var upcoming = new List<UpcomingCourseInput> { new("Course A", 10m) };

        var result = TargetGpaCalculator.Predict(0m, 0m, 4.0m, upcoming, maxScaleGpa: 4.0m);

        Assert.Equal(4.0m, result.RequiredAverageGpa);
        Assert.True(result.IsAchievable);
    }

    [Fact]
    public void Predict_EmptyUpcomingCourses_Throws()
    {
        Assert.Throws<DomainException>(
            () => TargetGpaCalculator.Predict(3.0m, 60m, 3.5m, [], maxScaleGpa: 4.0m));
    }

    [Fact]
    public void Predict_AllZeroFutureHours_Throws()
    {
        var upcoming = new List<UpcomingCourseInput> { new("Ghost Course", 0m) };

        Assert.Throws<DomainException>(
            () => TargetGpaCalculator.Predict(3.0m, 60m, 3.5m, upcoming, maxScaleGpa: 4.0m));
    }

    [Fact]
    public void Predict_NegativeBaseline_Throws()
    {
        var upcoming = new List<UpcomingCourseInput> { new("Course A", 10m) };

        Assert.Throws<DomainException>(
            () => TargetGpaCalculator.Predict(-1m, 60m, 3.5m, upcoming, maxScaleGpa: 4.0m));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Predict_NonPositiveMaxScaleGpa_Throws(double maxScaleGpa)
    {
        var upcoming = new List<UpcomingCourseInput> { new("Course A", 10m) };

        Assert.Throws<DomainException>(
            () => TargetGpaCalculator.Predict(3.0m, 60m, 3.5m, upcoming, (decimal)maxScaleGpa));
    }

    [Fact]
    public void Predict_TargetBelowCurrent_StillComputesLowerRequiredAverage()
    {
        var upcoming = new List<UpcomingCourseInput> { new("Course A", 30m) };

        var result = TargetGpaCalculator.Predict(3.2m, 60m, 3.0m, upcoming, maxScaleGpa: 4.0m);

        Assert.Equal((270m - 192m) / 30m, result.RequiredAverageGpa);
        Assert.True(result.IsAchievable);
    }
}
