using GPAHub.Domain.Enums;
using GPAHub.Domain.Exceptions;

namespace GPAHub.Domain.Entities;

public sealed class GpaRecord
{
    private readonly List<GpaRecordCourseLine> _courseLines = [];

    public Guid Id { get; private set; }

    public Guid StudentId { get; private set; }

    public CalculationType CalculationType { get; private set; }

    public decimal SemesterGpa { get; private set; }

    public decimal? CumulativeGpa { get; private set; }

    public decimal TotalCreditHours { get; private set; }

    public decimal TotalQualityPoints { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public IReadOnlyList<GpaRecordCourseLine> CourseLines => _courseLines.AsReadOnly();

    private GpaRecord()
    {
    }

    public GpaRecord(
        Guid studentId,
        CalculationType calculationType,
        decimal semesterGpa,
        decimal? cumulativeGpa,
        decimal totalCreditHours,
        decimal totalQualityPoints,
        DateTimeOffset createdAtUtc)
    {
        if (studentId == Guid.Empty)
        {
            throw new DomainException("Student id is required.");
        }

        if (semesterGpa < 0m)
        {
            throw new DomainException("Semester GPA cannot be negative.");
        }

        if (cumulativeGpa.HasValue && cumulativeGpa.Value < 0m)
        {
            throw new DomainException("Cumulative GPA cannot be negative.");
        }

        if (totalCreditHours <= 0m)
        {
            throw new DomainException("Total credit hours must be greater than zero.");
        }

        if (totalQualityPoints < 0m)
        {
            throw new DomainException("Total quality points cannot be negative.");
        }

        Id = Guid.NewGuid();
        StudentId = studentId;
        CalculationType = calculationType;
        SemesterGpa = semesterGpa;
        CumulativeGpa = cumulativeGpa;
        TotalCreditHours = totalCreditHours;
        TotalQualityPoints = totalQualityPoints;
        CreatedAtUtc = createdAtUtc;
    }

    public void AddLine(string courseName, string? courseCode, decimal creditHours, string gradeName, decimal gpaPoints)
    {
        _courseLines.Add(new GpaRecordCourseLine(courseName, courseCode, creditHours, gradeName, gpaPoints));
    }
}
