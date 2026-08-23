namespace GPAHub.Application.DTOs.GradeScale;

public sealed record GradeDefinitionItemDto(
    Guid Id,
    string Name,
    int MinMark,
    int MaxMark,
    decimal Points);

public sealed record GradeScaleDto(
    Guid Id,
    Guid? StudentId,
    string Name,
    string? Description,
    bool IsActive,
    bool EnforceFullCoverage,
    IReadOnlyList<GradeDefinitionItemDto> Definitions);

public sealed record SaveGradeDefinitionDto(string Name, int MinMark, int MaxMark, decimal Points);

public sealed record CreateGradeScaleDto(
    string Name,
    string? Description,
    bool EnforceFullCoverage = false);

public sealed record UpdateGradeScaleDto(
    string Name,
    string? Description,
    bool EnforceFullCoverage = false);
