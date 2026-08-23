using GPAHub.Domain.Exceptions;

namespace GPAHub.Domain.Entities;

public sealed class GpaRecordCourseLine
{
    public Guid Id { get; private set; }

    public Guid GpaRecordId { get; private set; }

    public string CourseName { get; private set; }

    public string? CourseCode { get; private set; }

    public decimal CreditHours { get; private set; }

    public string GradeName { get; private set; }

    public decimal GpaPoints { get; private set; }

    public decimal QualityPoints { get; private set; }

    private GpaRecordCourseLine()
    {
        CourseName = string.Empty;
        GradeName = string.Empty;
    }

    internal GpaRecordCourseLine(string courseName, string? courseCode, decimal creditHours, string gradeName, decimal gpaPoints)
        : this(Guid.NewGuid(), Guid.Empty, courseName, courseCode, creditHours, gradeName, gpaPoints)
    {
    }

    internal GpaRecordCourseLine(Guid id, Guid gpaRecordId, string courseName, string? courseCode, decimal creditHours, string gradeName, decimal gpaPoints)
    {
        if (string.IsNullOrWhiteSpace(courseName))
        {
            throw new DomainException("Course name is required in a GPA record line.");
        }

        if (string.IsNullOrWhiteSpace(gradeName))
        {
            throw new DomainException("Grade name is required in a GPA record line.");
        }

        var hours = new ValueObjects.CreditHours(creditHours);

        if (gpaPoints < 0m)
        {
            throw new DomainException("GPA points cannot be negative.");
        }

        Id = id;
        GpaRecordId = gpaRecordId;
        CourseName = courseName.Trim();
        CourseCode = string.IsNullOrWhiteSpace(courseCode) ? null : courseCode.Trim();
        CreditHours = hours.Value;
        GradeName = gradeName.Trim();
        GpaPoints = gpaPoints;
        QualityPoints = gpaPoints * hours.Value;
    }
}
