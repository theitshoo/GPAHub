using GPAHub.Domain.Enums;

namespace GPAHub.Application.DTOs.Course;

public sealed record CourseDto(
    Guid Id,
    Guid StudentId,
    Guid? SemesterId,
    string Name,
    string? Code,
    decimal CreditHours,
    GradeInputType InputType,
    int? NumericMark,
    string? LetterGrade);

public sealed record CourseInputDto(
    string Name,
    string? Code,
    decimal CreditHours,
    GradeInputType InputType,
    int? NumericMark,
    string? LetterGrade);

public sealed record SemesterOptionDto(Guid Id, string Name);
