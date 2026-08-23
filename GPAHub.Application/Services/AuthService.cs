using System.Security.Claims;
using System.Security.Cryptography;
using GPAHub.Application.Common;
using GPAHub.Application.DTOs.Student;
using GPAHub.Application.Interfaces.Repositories;
using GPAHub.Application.Interfaces.Services;
using GPAHub.Application.Validators;
using GPAHub.Domain.Constants;
using GPAHub.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace GPAHub.Application.Services;

public class AuthService : IAuthService
{
    public const string InvalidCredentialsCode = "invalid_credentials";
    public const string InvalidRefreshTokenCode = "invalid_refresh_token";

    private readonly IStudentRepository _repository;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITokenService _tokenService;
    private readonly IPasswordHasher<Student> _passwordHasher;
    private readonly ILogger<AuthService> _logger;
    private readonly RegisterStudentDtoValidator _registerValidator = new();
    private readonly LoginRequestDtoValidator _loginValidator = new();

    public AuthService(
        IStudentRepository repository,
        IRefreshTokenRepository refreshTokenRepository,
        IUnitOfWork unitOfWork,
        ITokenService tokenService,
        IPasswordHasher<Student> passwordHasher,
        RegisterStudentDtoValidator registerValidator,
        LoginRequestDtoValidator loginValidator,
        ILogger<AuthService> logger)
    {
        _repository = repository;
        _refreshTokenRepository = refreshTokenRepository;
        _unitOfWork = unitOfWork;
        _tokenService = tokenService;
        _passwordHasher = passwordHasher;
        _registerValidator = registerValidator;
        _loginValidator = loginValidator;
        _logger = logger;
    }

    public async Task<Result<AuthResponseDto>> RegisterAsync(RegisterStudentDto dto, CancellationToken cancellationToken = default)
    {
        var validation = await _registerValidator.ValidateAsync(dto, cancellationToken);
        if (!validation.IsValid)
        {
            return Result<AuthResponseDto>.Fail(ValidationErrors.From(validation));
        }

        if (await _repository.ExistsByEmailAsync(dto.Email, cancellationToken))
        {
            _logger.LogWarning("Registration rejected: email already in use");
            return Result<AuthResponseDto>.Fail(Error.Conflict("email_taken", "An account with this email already exists."));
        }

        var student = new Student(dto.Name, dto.Email);

        student.SetPasswordHash(_passwordHasher.HashPassword(student, dto.Password));

        await _repository.AddAsync(student, cancellationToken);

        var response = await IssueSessionAsync(student, cancellationToken);
        if (response.IsFailure)
        {
            return Result<AuthResponseDto>.Fail(response.Error!);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("New student registered: {StudentId}", student.Id);

        return response;
    }

    public async Task<Result<AuthResponseDto>> LoginAsync(LoginRequestDto dto, CancellationToken cancellationToken = default)
    {
        var validation = await _loginValidator.ValidateAsync(dto, cancellationToken);
        if (!validation.IsValid)
        {
            return Result<AuthResponseDto>.Fail(ValidationErrors.From(validation));
        }

        var student = await _repository.GetByEmailAsync(dto.Email, cancellationToken);

        if (student?.PasswordHash is null ||
            _passwordHasher.VerifyHashedPassword(student, student.PasswordHash, dto.Password) == PasswordVerificationResult.Failed)
        {
            _logger.LogWarning("Failed login attempt (invalid credentials)");
            return Result<AuthResponseDto>.Fail(Error.Unauthorized(InvalidCredentialsCode, "Invalid email or password."));
        }

        var response = await IssueSessionAsync(student, cancellationToken);
        if (response.IsFailure)
        {
            return Result<AuthResponseDto>.Fail(response.Error!);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Student logged in: {StudentId}", student.Id);

        return response;
    }

    public async Task<Result<AuthResponseDto>> RefreshAsync(RefreshRequestDto dto, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(dto.RefreshToken))
        {
            return Result<AuthResponseDto>.Fail(Error.Unauthorized(InvalidRefreshTokenCode, "Invalid refresh token."));
        }

        var stored = await _refreshTokenRepository.GetByTokenHashAsync(HashToken(dto.RefreshToken), cancellationToken);

        if (stored is null)
        {
            return Result<AuthResponseDto>.Fail(Error.Unauthorized(InvalidRefreshTokenCode, "Invalid refresh token."));
        }

        var now = DateTimeOffset.UtcNow;

        if (!stored.IsAliveAsOf(now))
        {
            if (stored.RevokedAtUtc is not null)
            {
                _logger.LogWarning(
                    "Refresh token reuse detected for student {StudentId}; revoking all active sessions",
                    stored.StudentId);

                await _refreshTokenRepository.RevokeAllActiveForStudentAsync(stored.StudentId, now, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }

            return Result<AuthResponseDto>.Fail(Error.Unauthorized(InvalidRefreshTokenCode, "Invalid refresh token."));
        }

        var student = await _repository.GetByIdAsync(stored.StudentId, cancellationToken);
        if (student is null)
        {
            return Result<AuthResponseDto>.Fail(Error.Unauthorized(InvalidRefreshTokenCode, "Invalid refresh token."));
        }

        stored.Revoke(now);

        var response = await IssueSessionAsync(student, cancellationToken);
        if (response.IsFailure)
        {
            return Result<AuthResponseDto>.Fail(response.Error!);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return response;
    }

    public async Task<Result> LogoutAsync(LogoutRequestDto dto, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(dto.RefreshToken))
        {
            return Result.Fail(Error.Unauthorized(InvalidRefreshTokenCode, "Invalid refresh token."));
        }

        var stored = await _refreshTokenRepository.GetByTokenHashAsync(HashToken(dto.RefreshToken), cancellationToken);

        if (stored is { } && stored.IsAliveAsOf(DateTimeOffset.UtcNow))
        {
            stored.Revoke(DateTimeOffset.UtcNow);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        return Result.Ok();
    }

    private async Task<Result<AuthResponseDto>> IssueSessionAsync(Student student, CancellationToken cancellationToken)
    {
        var refreshTokenValue = GenerateSecureRefreshToken();
        var now = DateTimeOffset.UtcNow;

        var token = new RefreshToken(
            student.Id,
            HashToken(refreshTokenValue),
            now,
            now.AddDays(RefreshTokenDefaults.LifetimeDays));

        await _refreshTokenRepository.AddAsync(token, cancellationToken);

        return Result<AuthResponseDto>.Ok(new AuthResponseDto(
            student.Id,
            student.Name,
            student.Email,
            AccessToken: _tokenService.GenerateToken(student),
            RefreshToken: refreshTokenValue));
    }

    private static string GenerateSecureRefreshToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(64);
        return Convert.ToBase64String(bytes);
    }

    private static string HashToken(string token)
    {
        var hash = SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(token));
        return Convert.ToHexString(hash);
    }
}
