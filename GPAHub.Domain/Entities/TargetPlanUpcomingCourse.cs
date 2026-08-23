using GPAHub.Domain.Exceptions;

namespace GPAHub.Domain.Entities;

public sealed class TargetPlanUpcomingCourse
{
    public Guid Id { get; private set; }

    public Guid TargetPlanId { get; private set; }

    public string Name { get; private set; }

    public decimal CreditHours { get; private set; }

    private TargetPlanUpcomingCourse()
    {
        Name = string.Empty;
    }

    internal TargetPlanUpcomingCourse(string name, decimal creditHours)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainException("Upcoming course name is required.");
        }

        var hours = new ValueObjects.CreditHours(creditHours);

        Id = Guid.NewGuid();
        Name = name.Trim();
        CreditHours = hours.Value;
    }
}
