using GPAHub.Domain.Constants;
using GPAHub.Domain.Enums;
using GPAHub.Domain.Exceptions;

namespace GPAHub.Domain.Entities;

public sealed class Course
{
    public Guid Id { get; private set; }

    public Guid StudentId { get; private set; }

    public Guid? SemesterId { get; private set; }

    public string Name { get; private set; }

    public string? Code { get; private set; }

    public ValueObjects.CreditHours CreditHours { get; private set; }

    public GradeInputType InputType { get; private set; }

    public int? NumericMark { get; private set; }

    public string? LetterGrade { get; private set; }

    private Course()
    {
        Name = string.Empty;
        CreditHours = new ValueObjects.CreditHours(1m);
    }

    private Course(Guid studentId, string name, string? code, decimal creditHours)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainException("Course name is required.");
        }

        Id = Guid.NewGuid();
        StudentId = studentId;
        Name = name.Trim();
        Code = NormalizeCode(code);
        CreditHours = new ValueObjects.CreditHours(creditHours);
    }

    public static Course CreateNumeric(Guid studentId, string name, string? code, decimal creditHours, int numericMark)
    {
        var course = new Course(studentId, name, code, creditHours);

        course.UpdateAsNumeric(numericMark);

        return course;
    }

    public static Course CreateLetterGrade(Guid studentId, string name, string? code, decimal creditHours, string letterGrade)
    {
        var course = new Course(studentId, name, code, creditHours);

        course.UpdateAsLetter(letterGrade);

        return course;
    }

    public void UpdateDetails(string name, string? code, decimal creditHours)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainException("Course name is required.");
        }

        Name = name.Trim();
        Code = NormalizeCode(code);
        CreditHours = new ValueObjects.CreditHours(creditHours);
    }

    public void UpdateAsNumeric(int numericMark)
    {
        if (numericMark < MarkRange.AbsoluteMinimum || numericMark > MarkRange.AbsoluteMaximum)
        {
            throw new DomainException(
                $"Numeric mark must be between {MarkRange.AbsoluteMinimum} and {MarkRange.AbsoluteMaximum}.");
        }

        InputType = GradeInputType.NumericMark;
        NumericMark = numericMark;
        LetterGrade = null;
    }

    public void UpdateAsLetter(string letterGrade)
    {
        if (string.IsNullOrWhiteSpace(letterGrade))
        {
            throw new DomainException("Letter grade is required when input type is letter grade.");
        }

        InputType = GradeInputType.LetterGrade;
        LetterGrade = letterGrade.Trim();
        NumericMark = null;
    }

    public void AssignToSemester(Guid semesterId) => SemesterId = semesterId;

    public void RemoveFromSemester() => SemesterId = null;

    private static string? NormalizeCode(string? code) =>
        string.IsNullOrWhiteSpace(code) ? null : code.Trim();
}
