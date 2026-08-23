using GPAHub.Application.DTOs.Gpa;
using GPAHub.Application.DTOs.Student;
using GPAHub.Application.DTOs.Target;

namespace GPAHub.Application.DTOs.Report;

public sealed record TargetReportSectionDto(
    decimal TargetGpa,
    decimal RequiredAverageGpa,
    bool IsAchievable,
    decimal? MaxReachableGpa);

public sealed record ReportDto(
    string Title,
    string Tagline,
    DateTimeOffset GeneratedAtUtc,
    AcademicBaselineDto? Baseline,
    IReadOnlyList<GpaCourseResultDto> Courses,
    decimal? SemesterGpa,
    decimal? CumulativeGpa,
    TargetReportSectionDto? TargetAnalysis,
    IReadOnlyList<GradeCombinationDto>? GradeSuggestions);
