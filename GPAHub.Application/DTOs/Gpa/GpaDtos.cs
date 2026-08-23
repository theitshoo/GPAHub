using GPAHub.Application.DTOs.GradeScale;

namespace GPAHub.Application.DTOs.Gpa;

public sealed record GpaCourseInputDto(
    string? Name,
    decimal CreditHours,
    Domain.Enums.GradeInputType InputType,
    int? NumericMark,
    string? LetterGrade);

public sealed record CalculateGpaRequestDto(
    IReadOnlyList<GpaCourseInputDto> Courses,
    decimal? BaselineGpa,
    decimal? BaselineCreditHours,
    IReadOnlyList<SaveGradeDefinitionDto>? CustomScaleDefinitions = null,
    Guid? ScaleId = null);

public sealed record GpaCourseResultDto(
    string? Name,
    decimal CreditHours,
    string GradeName,
    decimal GpaPoints,
    decimal QualityPoints);

public sealed record GpaCalculationResponseDto(
    decimal TotalCreditHours,
    decimal TotalQualityPoints,
    decimal SemesterGpa,
    decimal? CumulativeGpa,
    string UsedScaleName,
    IReadOnlyList<GpaCourseResultDto> CourseResults);
