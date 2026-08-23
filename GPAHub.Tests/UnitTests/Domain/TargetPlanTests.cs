using GPAHub.Domain.Entities;
using GPAHub.Domain.Exceptions;

namespace GPAHub.Tests.UnitTests.Domain;

public class TargetPlanTests
{
    private static readonly DateTimeOffset FixedTime = new(2026, 8, 23, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Constructor_WithValidAchievablePlan_StoresAllOutputs()
    {
        var plan = new TargetPlan(
            Guid.NewGuid(),
            targetGpa: 3.8m, currentGpa: 3.2m, completedCreditHours: 60m,
            requiredAverageGpa: 4.0m, isAchievable: true, maxReachableGpa: 4.0m,
            createdAtUtc: FixedTime);

        Assert.True(plan.IsAchievable);
        Assert.Equal(4.0m, plan.RequiredAverageGpa);
        Assert.Equal(4.0m, plan.MaxReachableGpa);
        Assert.Empty(plan.UpcomingCourses);
    }

    [Fact]
    public void Constructor_InfeasiblePlan_MayOmitMaxReachable()
    {
        var plan = new TargetPlan(Guid.NewGuid(), 4.0m, 1.0m, 90m, 6.0m, false, null, FixedTime);

        Assert.False(plan.IsAchievable);
        Assert.Null(plan.MaxReachableGpa);
    }

    [Theory]
    [InlineData(-0.1, 3.0, 30)]
    [InlineData(3.8, -1, 30)]
    [InlineData(3.8, 3.0, -5)]
    public void Constructor_WithNegativeInputs_Throws(double target, double current, double completed)
    {
        Assert.Throws<DomainException>(
            () => new TargetPlan(Guid.NewGuid(), (decimal)target, (decimal)current, (decimal)completed, 3.5m, true, null, FixedTime));
    }

    [Fact]
    public void AddUpcomingCourse_StoresNameAndHours()
    {
        var plan = CreateValidPlan();

        plan.AddUpcomingCourse("Operating Systems", 3m);

        var course = plan.UpcomingCourses.Single();
        Assert.Equal("Operating Systems", course.Name);
        Assert.Equal(3m, course.CreditHours);
    }

    [Fact]
    public void AddUpcomingCourse_WithZeroHours_Throws()
    {
        var plan = CreateValidPlan();

        Assert.Throws<DomainException>(() => plan.AddUpcomingCourse("Anything", 0m));
    }

    [Fact]
    public void AddUpcomingCourse_WithEmptyName_Throws()
    {
        var plan = CreateValidPlan();

        Assert.Throws<DomainException>(() => plan.AddUpcomingCourse("", 3m));
    }

    private static TargetPlan CreateValidPlan() =>
        new(Guid.NewGuid(), 3.5m, 3.0m, 30m, 3.75m, true, 4.0m, FixedTime);
}
