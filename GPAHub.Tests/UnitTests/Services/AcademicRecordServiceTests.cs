using AutoMapper;
using GPAHub.Application.Common;
using GPAHub.Application.DTOs.Student;
using GPAHub.Application.Interfaces.Repositories;
using GPAHub.Application.Mappings;
using GPAHub.Application.Services;
using GPAHub.Application.Validators;
using GPAHub.Domain.Entities;
using GPAHub.Domain.Exceptions;
using Moq;

namespace GPAHub.Tests.UnitTests.Services;

public class AcademicRecordServiceTests
{
    private readonly Mock<IStudentRepository> _repo = new();
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly AcademicRecordService _service;

    public AcademicRecordServiceTests()
    {
        var mapper = new MapperConfiguration(cfg => cfg.AddProfile<MappingProfile>()).CreateMapper();
        _service = new AcademicRecordService(
            _repo.Object,
            _uow.Object,
            mapper,
            new UpdateProfileDtoValidator(),
            new UpdateBaselineDtoValidator());
    }

    [Fact]
    public async Task GetProfile_ReturnsMappedData()
    {
        var student = new Student("Ali", "ali@test.com");
        SetupStudent(student);

        var result = await _service.GetProfileAsync(student.Id);

        Assert.True(result.IsSuccess);
        Assert.Equal("ali@test.com", result.Value.Email);
        Assert.Null(result.Value.CurrentGpa);
    }

    [Fact]
    public async Task GetProfile_UnknownStudent_ReturnsNotFound()
    {
        _repo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Student?)null);

        var result = await _service.GetProfileAsync(Guid.NewGuid());

        Assert.Equal(ErrorType.NotFound, result.Error!.Type);
    }

    [Fact]
    public async Task UpdateBaseline_SavesBothValues()
    {
        var student = new Student("Ali", "ali@test.com");
        SetupStudent(student);

        var result = await _service.UpdateBaselineAsync(student.Id, new UpdateBaselineDto(3.2m, 45m));

        Assert.True(result.IsSuccess);
        Assert.Equal(3.2m, student.CurrentGpa);
        Assert.Equal(45m, student.CompletedCreditHours);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateBaseline_NegativeInput_FailsValidation_AndDoesNotSave()
    {
        var student = new Student("Ali", "ali@test.com");
        SetupStudent(student);

        var result = await _service.UpdateBaselineAsync(student.Id, new UpdateBaselineDto(-1m, 10m));

        Assert.Equal(ErrorType.Validation, result.Error!.Type);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ClearBaseline_NullsValues()
    {
        var student = new Student("Ali", "ali@test.com");
        student.UpdateBaseline(3m, 30m);
        SetupStudent(student);

        var result = await _service.ClearBaselineAsync(student.Id);

        Assert.True(result.IsSuccess);
        Assert.Null(student.CurrentGpa);
        Assert.Null(student.CompletedCreditHours);
    }

    [Fact]
    public async Task DomainViolation_OnRename_MapsToConflict()
    {
        var student = new Student("Ali", "ali@test.com");
        SetupStudent(student);

        var result = await _service.UpdateBaselineAsync(student.Id, new UpdateBaselineDto(-5m, 0m));

        Assert.True(result.IsFailure);
    }

    private void SetupStudent(Student student) =>
        _repo.Setup(r => r.GetByIdAsync(student.Id, It.IsAny<CancellationToken>())).ReturnsAsync(student);
}
