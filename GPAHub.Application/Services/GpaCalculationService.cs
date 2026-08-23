using AutoMapper;
using GPAHub.Application.Common;
using GPAHub.Application.DTOs.Gpa;
using GPAHub.Application.Interfaces.Repositories;
using GPAHub.Application.Interfaces.Services;
using GPAHub.Domain.DomainServices;
using GPAHub.Domain.Entities;
using GPAHub.Domain.Enums;
using GPAHub.Domain.Exceptions;
using GPAHub.Domain.ValueObjects;

namespace GPAHub.Application.Services;

public class GpaCalculationService : IGpaCalculationService
{
    private readonly IGradeScaleRepository _scaleRepository;
    private readonly IStudentRepository _studentRepository;
    private readonly IGpaRecordRepository _recordRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public GpaCalculationService(
        IGradeScaleRepository scaleRepository,
        IStudentRepository studentRepository,
        IGpaRecordRepository recordRepository,
        IUnitOfWork unitOfWork,
        IMapper mapper)
    {
        _scaleRepository = scaleRepository;
        _studentRepository = studentRepository;
        _recordRepository = recordRepository;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public Task<Result<GpaCalculationResponseDto>> CalculateAsync(CalculateGpaRequestDto request, CancellationToken cancellationToken = default) =>
        CalculateCoreAsync(request, studentId: null, saveToHistory: false, cancellationToken);

    public Task<Result<GpaCalculationResponseDto>> CalculateForStudentAsync(Guid studentId, CalculateGpaRequestDto request, CancellationToken cancellationToken = default) =>
        CalculateCoreAsync(request, studentId, saveToHistory: false, cancellationToken);

    public Task<Result<GpaCalculationResponseDto>> CalculateAndSaveAsync(Guid studentId, CalculateGpaRequestDto request, CancellationToken cancellationToken = default) =>
        CalculateCoreAsync(request, studentId, saveToHistory: true, cancellationToken);

    private async Task<Result<GpaCalculationResponseDto>> CalculateCoreAsync(
        CalculateGpaRequestDto request,
        Guid? studentId,
        bool saveToHistory,
        CancellationToken cancellationToken)
    {
        if (request.Courses is null || request.Courses.Count == 0)
        {
            return Result<GpaCalculationResponseDto>.Fail(
                Error.Validation("courses_required", "At least one course is required."));
        }

        var scaleResult = await ScaleResolver.ResolveAsync(
            _scaleRepository, studentId, request.CustomScaleDefinitions, request.ScaleId, cancellationToken);
        if (scaleResult.IsFailure)
        {
            return Result<GpaCalculationResponseDto>.Fail(scaleResult.Error!);
        }

        var scale = scaleResult.Value;

        var semesterInputs = new List<SemesterCourseInput>();
        var courseGrades = new List<(string? Name, decimal Hours, string GradeName, decimal Points)>();

        foreach (var course in request.Courses)
        {
            GradeDefinition? definition;

            if (course.InputType == GradeInputType.NumericMark)
            {
                definition = scale.FindDefinitionForMark(course.NumericMark!.Value);
                if (definition is null)
                {
                    return Result<GpaCalculationResponseDto>.Fail(Error.Validation(
                        "mark_not_covered",
                        $"Mark {course.NumericMark} for course '{course.Name ?? "unnamed"}' is not covered by the active grade scale."));
                }
            }
            else
            {
                definition = scale.FindDefinitionForGradeName(course.LetterGrade!);
                if (definition is null)
                {
                    return Result<GpaCalculationResponseDto>.Fail(Error.Validation(
                        "grade_not_in_scale",
                        $"Grade '{course.LetterGrade}' does not exist in the active grade scale."));
                }
            }

            semesterInputs.Add(new SemesterCourseInput(course.CreditHours, definition.Points));
            courseGrades.Add((course.Name, course.CreditHours, definition.Name, definition.Points));
        }

        SemesterGpaResult semester;
        decimal? rawCumulative = null;
        try
        {
            semester = GpaCalculator.CalculateSemester(semesterInputs);

            var baseline = await ResolveBaselineAsync(studentId, request, cancellationToken);
            if (baseline.HasValue)
            {
                var cumulative = GpaCalculator.CalculateCumulative(
                    baseline.Value.Gpa,
                    baseline.Value.Hours,
                    semester.TotalQualityPoints,
                    semester.TotalCreditHours);

                rawCumulative = cumulative.CumulativeGpa;
            }
        }
        catch (DomainException exception)
        {
            return Result<GpaCalculationResponseDto>.Fail(
                Error.Validation("calculation_invalid", exception.Message));
        }

        var response = new GpaCalculationResponseDto(
            TotalCreditHours: semester.TotalCreditHours,
            TotalQualityPoints: semester.TotalQualityPoints,
            SemesterGpa: Round(semester.SemesterGpa),
            CumulativeGpa: rawCumulative.HasValue ? Round(rawCumulative.Value) : null,
            UsedScaleName: scale.Name,
            CourseResults: courseGrades
                .Select(c => new GpaCourseResultDto(c.Name, c.Hours, c.GradeName, c.Points, c.Points * c.Hours))
                .ToList());

        if (!saveToHistory)
        {
            return Result<GpaCalculationResponseDto>.Ok(response);
        }

        await SaveHistoryAsync(studentId!.Value, semester, rawCumulative, response.CourseResults, cancellationToken);

        return Result<GpaCalculationResponseDto>.Ok(response);
    }

    private async Task<(decimal Gpa, decimal Hours)?> ResolveBaselineAsync(
        Guid? studentId,
        CalculateGpaRequestDto request,
        CancellationToken cancellationToken)
    {
        if (request.BaselineGpa.HasValue && request.BaselineCreditHours.HasValue)
        {
            return (request.BaselineGpa.Value, request.BaselineCreditHours.Value);
        }

        if (studentId.HasValue)
        {
            var student = await _studentRepository.GetByIdAsync(studentId.Value, cancellationToken);
            if (student?.CurrentGpa.HasValue == true && student.CompletedCreditHours.HasValue)
            {
                return (student.CurrentGpa.Value, student.CompletedCreditHours.Value);
            }
        }

        return null;
    }

    private async Task SaveHistoryAsync(
        Guid studentId,
        SemesterGpaResult semester,
        decimal? cumulativeGpa,
        IReadOnlyList<GpaCourseResultDto> courseResults,
        CancellationToken cancellationToken)
    {
        var record = new GpaRecord(
            studentId,
            CalculationType.Gpa,
            semester.SemesterGpa,
            cumulativeGpa,
            semester.TotalCreditHours,
            semester.TotalQualityPoints,
            DateTimeOffset.UtcNow);

        foreach (var line in courseResults)
        {
            record.AddLine(line.Name ?? "Unnamed course", courseCode: null, line.CreditHours, line.GradeName, line.GpaPoints);
        }

        await _recordRepository.AddAsync(record, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private static decimal Round(decimal value) => new GpaValue(value).Rounded;
}
