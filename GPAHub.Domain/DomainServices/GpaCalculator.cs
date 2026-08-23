using GPAHub.Domain.Exceptions;

namespace GPAHub.Domain.DomainServices;

public sealed record SemesterCourseInput(decimal CreditHours, decimal GpaPoints);

public sealed record SemesterGpaResult(
    decimal TotalCreditHours,
    decimal TotalQualityPoints,
    decimal SemesterGpa);

public static class GpaCalculator
{
    public static SemesterGpaResult CalculateSemester(IReadOnlyCollection<SemesterCourseInput> courses)
    {
        EnsureHasCourses(courses);
        ValidateInputs(courses);

        var totalCreditHours = courses.Sum(c => c.CreditHours);
        var totalQualityPoints = courses.Sum(c => c.GpaPoints * c.CreditHours);

        if (totalCreditHours <= 0m)
        {
            throw new DomainException("Total credit hours must be greater than zero to calculate a GPA.");
        }

        return new SemesterGpaResult(totalCreditHours, totalQualityPoints, totalQualityPoints / totalCreditHours);
    }

    public static CumulativeGpaResult CalculateCumulative(
        decimal previousGpa,
        decimal previousCompletedCreditHours,
        decimal currentSemesterQualityPoints,
        decimal currentSemesterCreditHours)
    {
        if (previousGpa < 0m)
        {
            throw new DomainException("Previous GPA cannot be negative.");
        }

        if (previousCompletedCreditHours < 0m)
        {
            throw new DomainException("Previous completed credit hours cannot be negative.");
        }

        if (currentSemesterQualityPoints < 0m || currentSemesterCreditHours < 0m)
        {
            throw new DomainException("Current semester values cannot be negative.");
        }

        var totalQualityPoints = previousGpa * previousCompletedCreditHours + currentSemesterQualityPoints;
        var totalCreditHours = previousCompletedCreditHours + currentSemesterCreditHours;

        if (totalCreditHours <= 0m)
        {
            throw new DomainException("Total credit hours must be greater than zero to calculate a cumulative GPA.");
        }

        return new CumulativeGpaResult(totalQualityPoints, totalCreditHours, totalQualityPoints / totalCreditHours);
    }

    private static void EnsureHasCourses(IReadOnlyCollection<SemesterCourseInput> courses)
    {
        if (courses is null || courses.Count == 0)
        {
            throw new DomainException("At least one course is required to calculate a GPA.");
        }
    }

    private static void ValidateInputs(IReadOnlyCollection<SemesterCourseInput> courses)
    {
        if (courses.Any(c => c.GpaPoints < 0m))
        {
            throw new DomainException("GPA points cannot be negative.");
        }

        if (courses.Any(c => c.CreditHours < 0m))
        {
            throw new DomainException("Credit hours cannot be negative.");
        }
    }
}

public sealed record CumulativeGpaResult(
    decimal TotalQualityPoints,
    decimal TotalCreditHours,
    decimal CumulativeGpa);
