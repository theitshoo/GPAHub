using AutoMapper;
using GPAHub.Application.Common;
using GPAHub.Application.DTOs.Course;
using GPAHub.Application.Interfaces.Repositories;
using GPAHub.Application.Mappings;
using GPAHub.Application.Services;
using GPAHub.Application.Validators;
using GPAHub.Domain.Entities;
using GPAHub.Domain.Enums;
using Moq;

namespace GPAHub.Tests.UnitTests.Services;

public class CourseServiceTests
{
    private readonly Mock<ICourseRepository> _repo = new();
    private readonly Mock<ISemesterRepository> _semesterRepo = new();
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly CourseService _service;

    public CourseServiceTests()
    {
        var mapper = new MapperConfiguration(cfg => cfg.AddProfile<MappingProfile>()).CreateMapper();
        _service = new CourseService(_repo.Object, _semesterRepo.Object, _uow.Object, mapper, new CourseInputDtoValidator());
    }

    [Fact]
    public async Task Create_NumericCourse_SavesAndReturns()
    {
        var studentId = Guid.NewGuid();

        var result = await _service.CreateAsync(studentId, new CourseInputDto("Math", "M1", 3m, GradeInputType.NumericMark, 90, null));

        Assert.True(result.IsSuccess);
        Assert.Equal(GradeInputType.NumericMark, result.Value.InputType);
        Assert.Equal(90, result.Value.NumericMark);
        _repo.Verify(r => r.AddAsync(It.IsAny<Course>(), It.IsAny<CancellationToken>()), Times.Once);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Create_LetterCourse_StoresGradeAsGiven()
    {
        var result = await _service.CreateAsync(Guid.NewGuid(), new CourseInputDto("Art", null, 2m, GradeInputType.LetterGrade, null, "b+"));

        Assert.True(result.IsSuccess);
        Assert.Equal("b+", result.Value.LetterGrade);
        Assert.Null(result.Value.NumericMark);
    }

    [Fact]
    public async Task Create_WithInvalidDto_FailsValidation_WithoutSaving()
    {
        var result = await _service.CreateAsync(Guid.NewGuid(), new CourseInputDto("", null, 0m, GradeInputType.NumericMark, null, null));

        Assert.Equal(ErrorType.Validation, result.Error!.Type);
        _repo.Verify(r => r.AddAsync(It.IsAny<Course>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Update_OnAnotherStudentsCourse_ReturnsNotFound()
    {
        _repo.Setup(r => r.GetByIdForStudentAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Course?)null);

        var result = await _service.UpdateAsync(Guid.NewGuid(), Guid.NewGuid(), NumericInput());

        Assert.Equal(ErrorType.NotFound, result.Error!.Type);
    }

    [Fact]
    public async Task Update_SwitchesInputTypeAtomically()
    {
        var course = Course.CreateLetterGrade(Guid.NewGuid(), "Art", null, 2m, "B");
        SetupFound(course);

        var result = await _service.UpdateAsync(course.StudentId, course.Id,
            new CourseInputDto("Art", null, 2m, GradeInputType.NumericMark, 95, null));

        Assert.True(result.IsSuccess);
        Assert.Equal(GradeInputType.NumericMark, course.InputType);
        Assert.Null(course.LetterGrade);
        Assert.Equal(95, course.NumericMark);
    }

    [Fact]
    public async Task Delete_RemovesOwnedCourse()
    {
        var course = Course.CreateNumeric(Guid.NewGuid(), "X", null, 3m, 70);
        SetupFound(course);

        var result = await _service.DeleteAsync(course.StudentId, course.Id);

        Assert.True(result.IsSuccess);
        _repo.Verify(r => r.Remove(course), Times.Once);
    }

    [Fact]
    public async Task List_FiltersBySemester_WhenProvided()
    {
        Guid? semesterId = Guid.NewGuid();
        _repo.Setup(r => r.ListByStudentAsync(It.IsAny<Guid>(), semesterId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var result = await _service.ListByStudentAsync(Guid.NewGuid(), semesterId);

        Assert.True(result.IsSuccess);
        _repo.Verify(r => r.ListByStudentAsync(It.IsAny<Guid>(), semesterId, It.IsAny<CancellationToken>()), Times.Once);
    }

    private static CourseInputDto NumericInput() => new("Math", null, 3m, GradeInputType.NumericMark, 80, null);

    private void SetupFound(Course course) =>
        _repo.Setup(r => r.GetByIdForStudentAsync(course.Id, course.StudentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(course);
}
