using AutoMapper;
using GPAHub.Application.Common;
using GPAHub.Application.DTOs.GradeScale;
using GPAHub.Application.DTOs.Gpa;
using GPAHub.Application.DTOs.Target;
using GPAHub.Application.Interfaces.Repositories;
using GPAHub.Application.Interfaces.Services;
using GPAHub.Application.Mappings;
using GPAHub.Application.Services;
using GPAHub.Domain.Entities;
using GPAHub.Domain.Enums;
using Moq;

namespace GPAHub.Tests.UnitTests.Services;

public class GpaCalculationServiceTests
{
    private readonly Mock<IGradeScaleRepository> _scaleRepo = new();
    private readonly Mock<IStudentRepository> _studentRepo = new();
    private readonly Mock<IGpaRecordRepository> _recordRepo = new();
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly GpaCalculationService _service;

    public GpaCalculationServiceTests()
    {
        var mapper = new MapperConfiguration(cfg => cfg.AddProfile<MappingProfile>()).CreateMapper();
        _service = new GpaCalculationService(
            _scaleRepo.Object, _studentRepo.Object, _recordRepo.Object, _uow.Object, mapper);
    }

    [Fact]
    public async Task Calculate_GuestWithCustomScale_ComputesSemesterGpa()
    {
        var request = BuildRequest(
            courses: [Course(3m, mark: 90), Course(3m, grade: "B")],
            custom: [new("A", 85, 100, 4m), new("B", 70, 84, 3m)]);

        var result = await _service.CalculateAsync(request);

        Assert.True(result.IsSuccess);
        Assert.Equal(21m, result.Value.TotalQualityPoints);
        Assert.Equal(6m, result.Value.TotalCreditHours);
        Assert.Equal(3.5m, result.Value.SemesterGpa);
        Assert.Equal("Custom Scale", result.Value.UsedScaleName);
    }

    [Fact]
    public async Task Calculate_WithBaseline_BlendsCumulative_AndRoundsToTwoDecimals()
    {
        var request = BuildRequest(
            courses: [Course(9m, mark: 95)],
            baselineGpa: 3.2m,
            baselineHours: 60m,
            custom: [new("A", 0, 100, 4m)]);

        var result = await _service.CalculateAsync(request);

        Assert.True(result.IsSuccess);
        Assert.Equal(36m, result.Value.TotalQualityPoints);
        Assert.Equal(3.30m, result.Value.CumulativeGpa!.Value);
    }

    [Fact]
    public async Task Calculate_MarkNotCoveredByScale_ReturnsValidation()
    {
        var request = BuildRequest(
            courses: [Course(3m, mark: 65)],
            custom: [new("A", 80, 100, 4m)]);

        var result = await _service.CalculateAsync(request);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.Error!.Type);
        Assert.Contains("mark_not_covered", result.Error.Code);
    }

    [Fact]
    public async Task Calculate_UnknownLetterGrade_ReturnsValidation()
    {
        var request = BuildRequest(
            courses: [Course(3m, grade: "Q")],
            custom: [new("A", 0, 100, 4m)]);

        var result = await _service.CalculateAsync(request);

        Assert.True(result.IsFailure);
        Assert.Contains("grade_not_in_scale", result.Error.Code);
    }

    [Fact]
    public async Task CalculateForStudent_WithoutRequestBaseline_UsesStoredBaseline()
    {
        var student = new Student("S", "s@t.com");
        student.UpdateBaseline(2.0m, 30m);
        _studentRepo.Setup(r => r.GetByIdAsync(student.Id, It.IsAny<CancellationToken>())).ReturnsAsync(student);
        SetupActiveScale(student.Id);

        var request = BuildRequest(courses: [Course(10m, mark: 50)], custom: null);
        var result = await _service.CalculateForStudentAsync(student.Id, request);

        Assert.True(result.IsSuccess);
        Assert.Equal((2.0m * 30m + 20m) / 40m, result.Value.CumulativeGpa!.Value);
    }

    [Fact]
    public async Task CalculateAndSave_PersistsRecordWithLines()
    {
        var studentId = Guid.NewGuid();
        SetupActiveScale(studentId);

        var request = BuildRequest(courses: [Course(3m, mark: 50)], custom: null);
        var result = await _service.CalculateAndSaveAsync(studentId, request);

        Assert.True(result.IsSuccess);
        _recordRepo.Verify(r => r.AddAsync(
            It.Is<GpaRecord>(rec =>
                rec.StudentId == studentId &&
                rec.CalculationType == CalculationType.Gpa &&
                rec.CourseLines.Count == 1),
            It.IsAny<CancellationToken>()), Times.Once);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CalculateAndSave_DoesNotTrustOrRequireClientResults()
    {
        var studentId = Guid.NewGuid();
        SetupActiveScale(studentId);

        var request = BuildRequest(courses: [Course(3m, mark: 50)], custom: null);
        var result = await _service.CalculateAndSaveAsync(studentId, request);

        Assert.Equal(2m, result.Value.SemesterGpa);
    }

    private static GpaCourseInputDto Course(decimal hours, int? mark = null, string? grade = null) =>
        mark.HasValue
            ? new GpaCourseInputDto("C", hours, GradeInputType.NumericMark, mark, null)
            : new GpaCourseInputDto("C", hours, GradeInputType.LetterGrade, null, grade);

    private static CalculateGpaRequestDto BuildRequest(
        IReadOnlyList<GpaCourseInputDto> courses,
        decimal? baselineGpa = null,
        decimal? baselineHours = null,
        List<SaveGradeDefinitionDto>? custom = null) =>
        new(courses, baselineGpa, baselineHours, custom, null);

    private void SetupActiveScale(Guid studentId)
    {
        var scale = new GradeScale("Active Scale", studentId);
        scale.AddDefinition("F", 0, 49, 1m);
        scale.AddDefinition("A", 50, 100, 2m);
        _scaleRepo.Setup(r => r.GetActiveForStudentAsync(studentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(scale);
    }
}

public class TargetGpaServiceTests
{
    private readonly Mock<IGradeScaleRepository> _scaleRepo = new();
    private readonly Mock<ITargetPlanRepository> _planRepo = new();
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<ISubscriptionService> _subscription = new();
    private readonly TargetGpaService _service;
    private readonly Guid StudentId = Guid.NewGuid();

    public TargetGpaServiceTests()
    {
        _service = new TargetGpaService(
            _scaleRepo.Object, _planRepo.Object, _uow.Object, _subscription.Object);
        SetupDefaultScale();
    }

    [Fact]
    public async Task Predict_AchievableTarget_ReturnsRequiredAverage()
    {
        var request = Request(target: 3.4m, upcoming: [("A", 15m), ("B", 15m)]);

        var result = await _service.PredictAsync(request, studentId: StudentId);

        Assert.True(result.IsSuccess);
        Assert.Equal(3.8m, result.Value.RequiredAverageGpa);
        Assert.True(result.Value.IsAchievable);
    }

    [Fact]
    public async Task Predict_InfeasibleTarget_ReportsMaxReachable()
    {
        var request = Request(target: 3.5m, upcoming: [("A", 30m)]);

        var result = await _service.PredictAsync(request, studentId: StudentId);

        Assert.False(result.Value.IsAchievable);
        Assert.Equal(
            Math.Round((192m + 4m * 30m) / 90m, 2, MidpointRounding.AwayFromZero),
            result.Value.MaxReachableGpa);
    }

    [Fact]
    public async Task FreeUser_RequestingCombinations_IsForbidden()
    {
        _subscription.Setup(s => s.IsPremiumAsync(StudentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var request = Request(target: 3.0m, upcoming: [("A", 3m)], includeCombinations: true);

        var result = await _service.PredictAsync(request, studentId: StudentId);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Forbidden, result.Error!.Type);
        Assert.Equal("premium_required", result.Error.Code);
    }

    [Fact]
    public async Task PremiumUser_RequestingCombinations_ReceivesThem()
    {
        _subscription.Setup(s => s.IsPremiumAsync(StudentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var request = Request(target: 3.0m, upcoming: [("Math", 3m), ("Art", 3m)], includeCombinations: true);

        var result = await _service.PredictAsync(request, studentId: StudentId);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value.Combinations);
        Assert.All(result.Value.Combinations!, c => Assert.True(c.ResultingGpa >= 3.0m));
    }

    [Fact]
    public async Task Guest_CannotRequestCombinations()
    {
        var request = Request(target: 3.0m, upcoming: [("A", 3m)], includeCombinations: true);

        var result = await _service.PredictAsync(request, studentId: null);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Forbidden, result.Error!.Type);
        _subscription.Verify(s => s.IsPremiumAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task PredictAndSave_PersistsPlanWithUpcomingCourses()
    {
        var request = Request(target: 3.0m, upcoming: [("Alpha", 3m), ("Beta", 4m)]);

        var result = await _service.PredictAndSaveAsync(StudentId, request);

        Assert.True(result.IsSuccess);
        _planRepo.Verify(r => r.AddAsync(
            It.Is<TargetPlan>(p =>
                p.StudentId == StudentId &&
                p.UpcomingCourses.Count == 2 &&
                p.RequiredAverageGpa == (3.0m * (60m + 7m) - 3.2m * 60m) / 7m),
            It.IsAny<CancellationToken>()), Times.Once);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task PremiumGate_BlocksEvenWhenSaving()
    {
        _subscription.Setup(s => s.IsPremiumAsync(StudentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var request = Request(target: 3.0m, upcoming: [("A", 3m)], includeCombinations: true);

        var result = await _service.PredictAndSaveAsync(StudentId, request);

        Assert.True(result.IsFailure);
        _planRepo.Verify(r => r.AddAsync(It.IsAny<TargetPlan>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    private static TargetPredictionRequestDto Request(
        decimal target,
        (string Name, decimal Hours)[] upcoming,
        bool includeCombinations = false) =>
        new(
            CurrentGpa: 3.2m,
            CompletedCreditHours: 60m,
            TargetGpa: target,
            UpcomingCourses: upcoming.Select(u => new UpcomingCourseInputDto(u.Name, u.Hours)).ToList(),
            IncludeCombinations: includeCombinations);

    private void SetupDefaultScale()
    {
        var scale = new GradeScale("System Default", null);
        scale.AddDefinition("F", 0, 59, 0m);
        scale.AddDefinition("C", 60, 69, 2m);
        scale.AddDefinition("B", 70, 79, 3m);
        scale.AddDefinition("A", 80, 100, 4m);
        _scaleRepo.Setup(r => r.GetSystemDefaultAsync(It.IsAny<CancellationToken>())).ReturnsAsync(scale);
    }
}
