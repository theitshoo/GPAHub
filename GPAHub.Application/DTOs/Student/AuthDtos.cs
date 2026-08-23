namespace GPAHub.Application.DTOs.Student;

public sealed record RegisterStudentDto(string Name, string Email, string Password);

public sealed record LoginRequestDto(string Email, string Password);

public sealed record RefreshRequestDto(string RefreshToken);

public sealed record LogoutRequestDto(string RefreshToken);

public sealed record AuthResponseDto(
    Guid StudentId,
    string Name,
    string Email,
    string AccessToken,
    string RefreshToken);
