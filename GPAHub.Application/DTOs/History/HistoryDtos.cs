using GPAHub.Domain.Enums;

namespace GPAHub.Application.DTOs.History;

public sealed record GpaRecordSummaryDto(
    Guid Id,
    CalculationType CalculationType,
    decimal SemesterGpa,
    decimal? CumulativeGpa,
    decimal TotalCreditHours,
    DateTimeOffset CreatedAtUtc);

public sealed record GpaRecordLineDto(
    string CourseName,
    string? CourseCode,
    decimal CreditHours,
    string GradeName,
    decimal GpaPoints,
    decimal QualityPoints);

public sealed record GpaRecordDetailDto(
    Guid Id,
    CalculationType CalculationType,
    decimal SemesterGpa,
    decimal? CumulativeGpa,
    decimal TotalCreditHours,
    decimal TotalQualityPoints,
    DateTimeOffset CreatedAtUtc,
    IReadOnlyList<GpaRecordLineDto> CourseLines);

public sealed record TargetPlanSummaryDto(
    Guid Id,
    decimal TargetGpa,
    decimal RequiredAverageGpa,
    bool IsAchievable,
    DateTimeOffset CreatedAtUtc);

public sealed record UpcomingCourseLineDto(string Name, decimal CreditHours);

public sealed record TargetPlanDetailDto(
    Guid Id,
    decimal TargetGpa,
    decimal CurrentGpa,
    decimal CompletedCreditHours,
    decimal RequiredAverageGpa,
    bool IsAchievable,
    decimal? MaxReachableGpa,
    DateTimeOffset CreatedAtUtc,
    IReadOnlyList<UpcomingCourseLineDto> UpcomingCourses);

public sealed record HistoryPageRequest(int Page = 1, int PageSize = 10);
