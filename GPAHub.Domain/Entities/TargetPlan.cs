using GPAHub.Domain.Exceptions;

namespace GPAHub.Domain.Entities;

public sealed class TargetPlan
{
    private readonly List<TargetPlanUpcomingCourse> _upcomingCourses = [];

    public Guid Id { get; private set; }

    public Guid StudentId { get; private set; }

    public decimal TargetGpa { get; private set; }

    public decimal CurrentGpa { get; private set; }

    public decimal CompletedCreditHours { get; private set; }

    public decimal RequiredAverageGpa { get; private set; }

    public bool IsAchievable { get; private set; }

    public decimal? MaxReachableGpa { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public IReadOnlyList<TargetPlanUpcomingCourse> UpcomingCourses => _upcomingCourses.AsReadOnly();

    private TargetPlan()
    {
    }

    public TargetPlan(
        Guid studentId,
        decimal targetGpa,
        decimal currentGpa,
        decimal completedCreditHours,
        decimal requiredAverageGpa,
        bool isAchievable,
        decimal? maxReachableGpa,
        DateTimeOffset createdAtUtc)
    {
        if (studentId == Guid.Empty)
        {
            throw new DomainException("Student id is required.");
        }

        if (targetGpa < 0m || currentGpa < 0m || completedCreditHours < 0m || requiredAverageGpa < 0m)
        {
            throw new DomainException("Target plan inputs cannot be negative.");
        }

        if (maxReachableGpa.HasValue && maxReachableGpa.Value < 0m)
        {
            throw new DomainException("Max reachable GPA cannot be negative.");
        }

        Id = Guid.NewGuid();
        StudentId = studentId;
        TargetGpa = targetGpa;
        CurrentGpa = currentGpa;
        CompletedCreditHours = completedCreditHours;
        RequiredAverageGpa = requiredAverageGpa;
        IsAchievable = isAchievable;
        MaxReachableGpa = maxReachableGpa;
        CreatedAtUtc = createdAtUtc;
    }

    public void AddUpcomingCourse(string courseName, decimal creditHours)
    {
        _upcomingCourses.Add(new TargetPlanUpcomingCourse(courseName, creditHours));
    }
}
