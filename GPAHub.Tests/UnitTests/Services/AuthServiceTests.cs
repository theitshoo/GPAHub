using AutoMapper;
using GPAHub.Application.Common;
using GPAHub.Application.DTOs.Student;
using GPAHub.Application.Interfaces.Repositories;
using GPAHub.Application.Interfaces.Services;
using GPAHub.Application.Mappings;
using GPAHub.Application.Services;
using GPAHub.Application.Validators;
using GPAHub.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Moq;

namespace GPAHub.Tests.UnitTests.Services;

public class AuthServiceTests
{
    private readonly Mock<IStudentRepository> _repo = new();
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<ITokenService> _tokens = new();
    private readonly AuthService _service;
    private readonly IPasswordHasher<Student> _hasher = new PasswordHasher<Student>();

    public AuthServiceTests()
    {
        var mapper = new MapperConfiguration(cfg => cfg.AddProfile<MappingProfile>()).CreateMapper();
        _service = new AuthService(
            _repo.Object,
            _uow.Object,
            _tokens.Object,
            _hasher,
            new RegisterStudentDtoValidator(),
            new LoginRequestDtoValidator());

        _tokens.Setup(t => t.GenerateToken(It.IsAny<Student>())).Returns("jwt-token");
    }

    [Fact]
    public async Task Register_StoresHashedPassword_NotPlaintext()
    {
        Student? captured = null;
        _repo.Setup(r => r.AddAsync(It.IsAny<Student>(), It.IsAny<CancellationToken>()))
            .Callback<Student, CancellationToken>((s, _) => captured = s)
            .Returns(Task.CompletedTask);

        var result = await _service.RegisterAsync(new RegisterStudentDto("Ali", "ali@test.com", "Passw0rd!"));

        Assert.True(result.IsSuccess);
        Assert.Equal("jwt-token", result.Value.AccessToken);
        Assert.Equal("ali@test.com", result.Value.Email);
        Assert.NotNull(captured!.PasswordHash);
        Assert.NotEqual("Passw0rd!", captured.PasswordHash);
        Assert.StartsWith("AQ", captured.PasswordHash);
    }

    [Fact]
    public async Task Register_DuplicateEmailDifferentCase_ReturnsConflict()
    {
        _repo.Setup(r => r.ExistsByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result = await _service.RegisterAsync(new RegisterStudentDto("Ali", "ALI@test.com", "Passw0rd!"));

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Conflict, result.Error!.Type);
        Assert.Equal("email_taken", result.Error.Code);
    }

    [Fact]
    public async Task Register_WeakPassword_FailsValidation_WithoutSaving()
    {
        var result = await _service.RegisterAsync(new RegisterStudentDto("Ali", "ali@test.com", "short"));

        Assert.Equal("password_too_short", result.Error!.Code);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Register_PasswordWithoutDigit_Fails()
    {
        var result = await _service.RegisterAsync(new RegisterStudentDto("Ali", "ali@test.com", "NoDigitsHere"));

        Assert.Equal("password_missing_digit", result.Error!.Code);
    }

    [Fact]
    public async Task Register_MalformedEmail_Fails()
    {
        var result = await _service.RegisterAsync(new RegisterStudentDto("Ali", "not-an-email", "Passw0rd!"));

        Assert.Equal("email_invalid", result.Error!.Code);
    }

    [Fact]
    public async Task Login_CorrectPassword_Succeeds()
    {
        var student = CreateStudentWithPassword("Passw0rd!");
        SetupStudent(student);

        var result = await _service.LoginAsync(new LoginRequestDto("ali@test.com", "Passw0rd!"));

        Assert.True(result.IsSuccess);
        Assert.Equal(student.Id, result.Value.StudentId);
    }

    [Fact]
    public async Task Login_UnknownEmail_And_WrongPassword_ProduceIdenticalErrors()
    {
        var student = CreateStudentWithPassword("Passw0rd!");
        SetupStudent(student);

        var unknownEmail = await _service.LoginAsync(new LoginRequestDto("ghost@test.com", "Whatever1"));
        var wrongPassword = await _service.LoginAsync(new LoginRequestDto("ali@test.com", "WrongPass1"));

        Assert.True(unknownEmail.IsFailure);
        Assert.True(wrongPassword.IsFailure);
        Assert.Equal(ErrorType.Unauthorized, unknownEmail.Error!.Type);
        Assert.Equal(unknownEmail.Error.Type, wrongPassword.Error!.Type);
        Assert.Equal(AuthService.InvalidCredentialsCode, wrongPassword.Error.Code);
        Assert.Equal(unknownEmail.Error.Message, wrongPassword.Error.Message);
    }

    [Fact]
    public async Task Login_AccountWithoutPassword_NeverSucceeds()
    {
        var student = new Student("Legacy", "legacy@test.com");
        SetupStudent(student);

        var result = await _service.LoginAsync(new LoginRequestDto("legacy@test.com", "Anything1"));

        Assert.Equal(AuthService.InvalidCredentialsCode, result.Error!.Code);
    }

    private Student CreateStudentWithPassword(string password)
    {
        var student = new Student("Ali", "ali@test.com");
        student.SetPasswordHash(_hasher.HashPassword(student, password));
        return student;
    }

    private void SetupStudent(Student student) =>
        _repo.Setup(r => r.GetByEmailAsync(student.Email, It.IsAny<CancellationToken>())).ReturnsAsync(student);
}
