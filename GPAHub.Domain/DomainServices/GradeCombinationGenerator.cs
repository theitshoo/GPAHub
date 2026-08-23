using GPAHub.Domain.Constants;
using GPAHub.Domain.Entities;
using GPAHub.Domain.Exceptions;

namespace GPAHub.Domain.DomainServices;

public sealed record GradeCombinationAssignment(string CourseName, string GradeName, decimal GpaPoints);

public sealed record GradeCombination(IReadOnlyList<GradeCombinationAssignment> Assignments, decimal ResultingGpa);

public sealed record GradeCombinationResult(IReadOnlyList<GradeCombination> Combinations, bool SearchWasTruncated);

public static class GradeCombinationGenerator
{
    public static GradeCombinationResult Generate(
        decimal currentQualityPoints,
        decimal completedCreditHours,
        decimal targetGpa,
        IReadOnlyCollection<UpcomingCourseInput> upcomingCourses,
        IReadOnlyList<GradeDefinition> availableGrades)
    {
        if (upcomingCourses is null || upcomingCourses.Count == 0)
        {
            throw new DomainException("At least one upcoming course is required to generate combinations.");
        }

        if (upcomingCourses.Count > CombinationLimits.MaxUpcomingCourses)
        {
            throw new DomainException(
                $"Grade combinations support at most {CombinationLimits.MaxUpcomingCourses} upcoming courses.");
        }

        if (availableGrades is null || availableGrades.Count == 0)
        {
            throw new DomainException("At least one grade definition is required to generate combinations.");
        }

        if (currentQualityPoints < 0m || completedCreditHours < 0m)
        {
            throw new DomainException("Academic baseline values cannot be negative.");
        }

        var orderedCourses = upcomingCourses.OrderBy(c => c.CreditHours).ToList();
        var futureCreditHours = orderedCourses.Sum(c => c.CreditHours);

        if (futureCreditHours <= 0m)
        {
            throw new DomainException("Total upcoming credit hours must be greater than zero.");
        }

        var totalCreditHours = completedCreditHours + futureCreditHours;
        var gradesByDescendingPoints = availableGrades
            .OrderByDescending(g => g.Points)
            .Select(g => (Grade: g.Name, g.Points))
            .ToArray();
        var bestFutureQualityPoints = gradesByDescendingPoints[0].Points * futureCreditHours;

        if ((currentQualityPoints + bestFutureQualityPoints) / totalCreditHours < targetGpa)
        {
            return new GradeCombinationResult([], false);
        }

        var combinations = new List<GradeCombination>();
        var assignments = new (string Name, decimal Hours, string Grade, decimal Points)[orderedCourses.Count];
        var evaluations = 0;
        var truncated = false;

        void Search(int courseIndex, decimal accumulatedFutureQualityPoints)
        {
            if (truncated || combinations.Count >= CombinationLimits.MaxResultsReturned)
            {
                return;
            }

            if (courseIndex == orderedCourses.Count)
            {
                evaluations++;
                var resultingGpa = (currentQualityPoints + accumulatedFutureQualityPoints) / totalCreditHours;

                if (resultingGpa >= targetGpa)
                {
                    combinations.Add(new GradeCombination(
                        assignments
                            .Select(a => new GradeCombinationAssignment(a.Name, a.Grade, a.Points))
                            .ToList(),
                        resultingGpa));
                }

                if (evaluations >= CombinationLimits.MaxCombinationsEvaluated)
                {
                    truncated = true;
                }

                return;
            }

            var course = orderedCourses[courseIndex];

            foreach (var grade in gradesByDescendingPoints)
            {
                var remainingBest = bestFutureQualityPoints - accumulatedFutureQualityPoints - course.CreditHours * grade.Points;
                var bestPossible = (currentQualityPoints + accumulatedFutureQualityPoints + course.CreditHours * grade.Points + remainingBest) / totalCreditHours;

                if (bestPossible < targetGpa)
                {
                    break;
                }

                assignments[courseIndex] = (course.Name, course.CreditHours, grade.Grade, grade.Points);
                Search(courseIndex + 1, accumulatedFutureQualityPoints + course.CreditHours * grade.Points);

                if (truncated || combinations.Count >= CombinationLimits.MaxResultsReturned)
                {
                    return;
                }
            }
        }

        Search(0, 0m);

        var orderedResults = combinations
            .OrderBy(c => c.ResultingGpa)
            .ThenBy(c => c.Assignments.Sum(a => a.GpaPoints))
            .ToList();

        return new GradeCombinationResult(orderedResults, truncated);
    }
}
