using GPAHub.Application.DTOs.Student;
using GPAHub.Application.Interfaces.Services;
using GPAHub.Web.Controllers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace GPAHub.Web.Controllers;

[EnableRateLimiting("auth")]
[Route("api/auth")]
public class AuthController : ApiControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("register")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<AuthResponseDto>> Register(RegisterStudentDto dto, CancellationToken cancellationToken) =>
        FromResult(await _authService.RegisterAsync(dto, cancellationToken));

    [HttpPost("login")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AuthResponseDto>> Login(LoginRequestDto dto, CancellationToken cancellationToken) =>
        FromResult(await _authService.LoginAsync(dto, cancellationToken));

    [HttpPost("refresh")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AuthResponseDto>> Refresh(RefreshRequestDto dto, CancellationToken cancellationToken) =>
        FromResult(await _authService.RefreshAsync(dto, cancellationToken));

    [HttpPost("logout")]
    public async Task<IActionResult> Logout(LogoutRequestDto dto, CancellationToken cancellationToken) =>
        FromResult(await _authService.LogoutAsync(dto, cancellationToken));
}
