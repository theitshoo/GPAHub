using GPAHub.Domain.Constants;
using GPAHub.Domain.Exceptions;

namespace GPAHub.Domain.Entities;

public sealed class GradeDefinition
{
    public Guid Id { get; private set; }

    public Guid GradeScaleId { get; private set; }

    public string Name { get; private set; }

    public int MinMark { get; private set; }

    public int MaxMark { get; private set; }

    public decimal Points { get; private set; }

    private GradeDefinition()
    {
        Name = string.Empty;
    }

    internal GradeDefinition(Guid id, Guid gradeScaleId, string name, int minMark, int maxMark, decimal points)
    {
        Validate(name, minMark, maxMark, points);

        Id = id;
        GradeScaleId = gradeScaleId;
        Name = name.Trim();
        MinMark = minMark;
        MaxMark = maxMark;
        Points = points;
    }

    public GradeDefinition(string name, int minMark, int maxMark, decimal points)
        : this(Guid.NewGuid(), Guid.Empty, name, minMark, maxMark, points)
    {
    }

    internal void Update(string name, int minMark, int maxMark, decimal points)
    {
        Validate(name, minMark, maxMark, points);

        Name = name.Trim();
        MinMark = minMark;
        MaxMark = maxMark;
        Points = points;
    }

    public bool Overlaps(GradeDefinition other) =>
        MinMark <= other.MaxMark && other.MinMark <= MaxMark;

    public bool HasSameNameAs(string name) =>
        string.Equals(Name, name.Trim(), StringComparison.OrdinalIgnoreCase);

    private static void Validate(string name, int minMark, int maxMark, decimal points)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainException("Grade name is required.");
        }

        if (minMark > maxMark)
        {
            throw new DomainException($"Grade '{name.Trim()}': minimum mark must be less than or equal to maximum mark.");
        }

        if (minMark < MarkRange.AbsoluteMinimum || maxMark > MarkRange.AbsoluteMaximum)
        {
            throw new DomainException(
                $"Grade '{name.Trim()}': marks must be between {MarkRange.AbsoluteMinimum} and {MarkRange.AbsoluteMaximum}.");
        }

        if (points < 0m)
        {
            throw new DomainException($"Grade '{name.Trim()}': GPA points cannot be negative.");
        }
    }
}
