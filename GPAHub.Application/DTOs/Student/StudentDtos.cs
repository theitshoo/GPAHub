namespace GPAHub.Application.DTOs.Student;

public sealed record StudentProfileDto(
    Guid Id,
    string Name,
    string Email,
    decimal? CurrentGpa,
    decimal? CompletedCreditHours);

public sealed record UpdateProfileDto(string Name);

public sealed record AcademicBaselineDto(decimal? CurrentGpa, decimal? CompletedCreditHours);

public sealed record UpdateBaselineDto(decimal CurrentGpa, decimal CompletedCreditHours);
