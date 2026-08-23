using GPAHub.Application.Common;
using GPAHub.Application.DTOs.Student;

namespace GPAHub.Application.Interfaces.Services;

public interface IAuthService
{
    Task<Result<AuthResponseDto>> RegisterAsync(RegisterStudentDto dto, CancellationToken cancellationToken = default);

    Task<Result<AuthResponseDto>> LoginAsync(LoginRequestDto dto, CancellationToken cancellationToken = default);
}
