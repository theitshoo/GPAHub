using GPAHub.Application.DTOs.Course;
using GPAHub.Application.DTOs.Gpa;
using GPAHub.Application.DTOs.Target;
using GPAHub.Domain.Enums;
using GPAHub.Application.Validators;

namespace GPAHub.Tests.UnitTests.Validators;

public class CourseAndCalculationValidatorsTests
{
    private readonly CourseInputDtoValidator _courseValidator = new();
    private readonly CalculateGpaRequestDtoValidator _calcValidator = new();
    private readonly TargetPredictionRequestDtoValidator _targetValidator = new();

    [Fact]
    public async Task Course_NumericMode_WithValidData_Passes()
    {
        var dto = new CourseInputDto("Math", "M1", 3m, GradeInputType.NumericMark, 87, null);

        var result = await _courseValidator.ValidateAsync(dto);

        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task Course_NumericMode_MissingMark_Fails()
    {
        var dto = new CourseInputDto("Math", null, 3m, GradeInputType.NumericMark, null, null);

        var result = await _courseValidator.ValidateAsync(dto);

        Assert.Contains(result.Errors, e => e.ErrorCode == "numeric_mark_required");
    }

    [Fact]
    public async Task Course_LetterMode_MissingGrade_Fails()
    {
        var dto = new CourseInputDto("Math", null, 3m, GradeInputType.LetterGrade, null, " ");

        var result = await _courseValidator.ValidateAsync(dto);

        Assert.Contains(result.Errors, e => e.ErrorCode == "letter_grade_required");
    }

    [Fact]
    public async Task Course_ZeroCreditHours_Fails()
    {
        var dto = new CourseInputDto("Math", null, 0m, GradeInputType.NumericMark, 80, null);

        var result = await _courseValidator.ValidateAsync(dto);

        Assert.Contains(result.Errors, e => e.ErrorCode == "credit_hours_positive");
    }

    [Fact]
    public async Task CalcRequest_EmptyCourses_Fails()
    {
        var dto = new CalculateGpaRequestDto([], null, null);

        var result = await _calcValidator.ValidateAsync(dto);

        Assert.Contains(result.Errors, e => e.ErrorCode == "courses_required");
    }

    [Fact]
    public async Task CalcRequest_HalfBaseline_Fails()
    {
        var courses = new List<GpaCourseInputDto> { new(null, 3m, GradeInputType.NumericMark, 90, null) };
        var dto = new CalculateGpaRequestDto(courses, 3.0m, null);

        var result = await _calcValidator.ValidateAsync(dto);

        Assert.Contains(result.Errors, e => e.ErrorCode == "baseline_incomplete");
    }

    [Fact]
    public async Task CalcRequest_ZeroHourEntriesAreAllowed_ForStatelessMath()
    {
        var courses = new List<GpaCourseInputDto>
        {
            new(null, 3m, GradeInputType.NumericMark, 90, null),
            new(null, 0m, GradeInputType.LetterGrade, null, "A")
        };
        var dto = new CalculateGpaRequestDto(courses, null, null);

        var result = await _calcValidator.ValidateAsync(dto);

        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task TargetRequest_EmptyUpcomingCourses_Fails()
    {
        var dto = new TargetPredictionRequestDto(3m, 30m, 3.5m, []);

        var result = await _targetValidator.ValidateAsync(dto);

        Assert.Contains(result.Errors, e => e.ErrorCode == "upcoming_courses_required");
    }

    [Fact]
    public async Task TargetRequest_ZeroHourUpcomingCourse_Fails()
    {
        var upcoming = new List<UpcomingCourseInputDto> { new("Ghost", 0m) };
        var dto = new TargetPredictionRequestDto(3m, 30m, 3.5m, upcoming);

        var result = await _targetValidator.ValidateAsync(dto);

        Assert.Contains(result.Errors, e => e.ErrorCode == "credit_hours_positive");
    }
}
