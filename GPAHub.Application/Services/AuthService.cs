using GPAHub.Application.Common;
using GPAHub.Application.DTOs.Student;
using GPAHub.Application.Interfaces.Repositories;
using GPAHub.Application.Interfaces.Services;
using GPAHub.Application.Validators;
using GPAHub.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace GPAHub.Application.Services;

public class AuthService : IAuthService
{
    public const string InvalidCredentialsCode = "invalid_credentials";

    private readonly IStudentRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITokenService _tokenService;
    private readonly IPasswordHasher<Student> _passwordHasher;
    private readonly ILogger<AuthService> _logger;
    private readonly RegisterStudentDtoValidator _registerValidator = new();
    private readonly LoginRequestDtoValidator _loginValidator = new();

    public AuthService(
        IStudentRepository repository,
        IUnitOfWork unitOfWork,
        ITokenService tokenService,
        IPasswordHasher<Student> passwordHasher,
        RegisterStudentDtoValidator registerValidator,
        LoginRequestDtoValidator loginValidator,
        ILogger<AuthService> logger)
    {
        _repository = repository;
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
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("New student registered: {StudentId}", student.Id);

        return Result<AuthResponseDto>.Ok(BuildAuthResponse(student));
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

        _logger.LogInformation("Student logged in: {StudentId}", student.Id);

        return Result<AuthResponseDto>.Ok(BuildAuthResponse(student));
    }

    private AuthResponseDto BuildAuthResponse(Student student) =>
        new(
            student.Id,
            student.Name,
            student.Email,
            AccessToken: _tokenService.GenerateToken(student));
}
