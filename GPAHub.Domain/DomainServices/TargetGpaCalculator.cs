using GPAHub.Domain.Exceptions;

namespace GPAHub.Domain.DomainServices;

public sealed record UpcomingCourseInput(string Name, decimal CreditHours);

public sealed record TargetPredictionResult(
    decimal CurrentQualityPoints,
    decimal TotalFutureCreditHours,
    decimal TotalCreditHoursAfterCompletion,
    decimal RequiredTotalQualityPoints,
    decimal RequiredFutureQualityPoints,
    decimal RequiredAverageGpa,
    bool IsAchievable,
    decimal MaxReachableGpa);

public static class TargetGpaCalculator
{
    public static TargetPredictionResult Predict(
        decimal currentGpa,
        decimal completedCreditHours,
        decimal targetGpa,
        IReadOnlyCollection<UpcomingCourseInput> upcomingCourses,
        decimal maxScaleGpa)
    {
        if (currentGpa < 0m || completedCreditHours < 0m)
        {
            throw new DomainException("Academic baseline values cannot be negative.");
        }

        if (upcomingCourses is null || upcomingCourses.Count == 0)
        {
            throw new DomainException("At least one upcoming course is required for a target prediction.");
        }

        if (upcomingCourses.Any(c => c.CreditHours < 0m))
        {
            throw new DomainException("Upcoming course credit hours cannot be negative.");
        }

        var totalFutureCreditHours = upcomingCourses.Sum(c => c.CreditHours);

        if (totalFutureCreditHours <= 0m)
        {
            throw new DomainException("Total upcoming credit hours must be greater than zero for a prediction.");
        }

        var currentQualityPoints = currentGpa * completedCreditHours;
        var totalCreditHoursAfterCompletion = completedCreditHours + totalFutureCreditHours;
        var requiredTotalQualityPoints = targetGpa * totalCreditHoursAfterCompletion;
        var requiredFutureQualityPoints = requiredTotalQualityPoints - currentQualityPoints;

        if (maxScaleGpa <= 0m)
        {
            throw new DomainException("Maximum scale GPA must be greater than zero.");
        }

        var requiredAverageGpa = requiredFutureQualityPoints / totalFutureCreditHours;
        var maxReachableGpa = (currentQualityPoints + maxScaleGpa * totalFutureCreditHours) / totalCreditHoursAfterCompletion;
        var isAchievable = requiredAverageGpa <= maxScaleGpa;

        return new TargetPredictionResult(
            currentQualityPoints,
            totalFutureCreditHours,
            totalCreditHoursAfterCompletion,
            requiredTotalQualityPoints,
            requiredFutureQualityPoints,
            requiredAverageGpa,
            isAchievable,
            maxReachableGpa);
    }
}
