using GPAHub.Application.DTOs.GradeScale;

namespace GPAHub.Application.DTOs.Target;

public sealed record UpcomingCourseInputDto(string Name, decimal CreditHours);

public sealed record TargetPredictionRequestDto(
    decimal CurrentGpa,
    decimal CompletedCreditHours,
    decimal TargetGpa,
    IReadOnlyList<UpcomingCourseInputDto> UpcomingCourses,
    bool IncludeCombinations = false,
    IReadOnlyList<SaveGradeDefinitionDto>? CustomScaleDefinitions = null,
    Guid? ScaleId = null);

public sealed record GradeCombinationAssignmentDto(
    string CourseName,
    string GradeName,
    decimal GpaPoints);

public sealed record GradeCombinationDto(
    IReadOnlyList<GradeCombinationAssignmentDto> Assignments,
    decimal ResultingGpa);

public sealed record TargetPredictionResponseDto(
    decimal CurrentQualityPoints,
    decimal TotalFutureCreditHours,
    decimal TotalCreditHoursAfterCompletion,
    decimal RequiredAverageGpa,
    bool IsAchievable,
    decimal MaxReachableGpa,
    string UsedScaleName,
    IReadOnlyList<GradeCombinationDto>? Combinations = null,
    bool? CombinationsTruncated = null);
