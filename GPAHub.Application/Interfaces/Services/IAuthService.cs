using GPAHub.Application.Common;
using GPAHub.Application.DTOs.Student;
using GPAHub.Application.Interfaces.Repositories;
using GPAHub.Application.Interfaces.Services;

namespace GPAHub.Application.Interfaces.Services;

public interface IAuthService
{
    Task<Result<AuthResponseDto>> RegisterAsync(RegisterStudentDto dto, CancellationToken cancellationToken = default);

    Task<Result<AuthResponseDto>> LoginAsync(LoginRequestDto dto, CancellationToken cancellationToken = default);

    Task<Result<AuthResponseDto>> RefreshAsync(RefreshRequestDto dto, CancellationToken cancellationToken = default);

    Task<Result> LogoutAsync(LogoutRequestDto dto, CancellationToken cancellationToken = default);
}
