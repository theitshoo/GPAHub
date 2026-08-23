using GPAHub.Application.Common;
using GPAHub.Application.DTOs.Gpa;
using GPAHub.Application.DTOs.Report;
using GPAHub.Application.DTOs.Student;
using GPAHub.Application.Interfaces.Repositories;
using GPAHub.Application.Interfaces.Services;
using GPAHub.Domain.Entities;
using GPAHub.Domain.ValueObjects;

namespace GPAHub.Application.Services;

public class ReportService : IReportService
{
    public const string SystemName = "GPAHub";

    public const string Tagline = "Your Academic Performance, All in One Place.";

    private readonly IGpaRecordRepository _recordRepository;
    private readonly ITargetPlanRepository _planRepository;
    private readonly IStudentRepository _studentRepository;

    public ReportService(
        IGpaRecordRepository recordRepository,
        ITargetPlanRepository planRepository,
        IStudentRepository studentRepository)
    {
        _recordRepository = recordRepository;
        _planRepository = planRepository;
        _studentRepository = studentRepository;
    }

    public async Task<Result<ReportDto>> BuildGpaReportAsync(Guid studentId, Guid recordId, CancellationToken cancellationToken = default)
    {
        var student = await _studentRepository.GetByIdAsync(studentId, cancellationToken);
        if (student is null)
        {
            return Result<ReportDto>.Fail(Error.NotFound("student_not_found", "Student was not found."));
        }

        var record = await _recordRepository.GetByIdForStudentAsync(recordId, studentId, cancellationToken);
        if (record is null)
        {
            return Result<ReportDto>.Fail(Error.NotFound("gpa_record_not_found", "GPA record was not found."));
        }

        var report = new ReportDto(
            Title: $"{SystemName} — GPA Calculation Report",
            Tagline,
            DateTimeOffset.UtcNow,
            Baseline: new AcademicBaselineDto(student.CurrentGpa, student.CompletedCreditHours),
            Courses: record.CourseLines
                .Select(l => new GpaCourseResultDto(l.CourseName, l.CreditHours, l.GradeName, l.GpaPoints, l.QualityPoints))
                .ToList(),
            SemesterGpa: Round(record.SemesterGpa),
            CumulativeGpa: record.CumulativeGpa.HasValue ? Round(record.CumulativeGpa.Value) : null,
            TargetAnalysis: null,
            GradeSuggestions: null);

        return Result<ReportDto>.Ok(report);
    }

    public async Task<Result<ReportDto>> BuildTargetReportAsync(Guid studentId, Guid planId, CancellationToken cancellationToken = default)
    {
        var student = await _studentRepository.GetByIdAsync(studentId, cancellationToken);
        if (student is null)
        {
            return Result<ReportDto>.Fail(Error.NotFound("student_not_found", "Student was not found."));
        }

        var plan = await _planRepository.GetByIdForStudentAsync(planId, studentId, cancellationToken);
        if (plan is null)
        {
            return Result<ReportDto>.Fail(Error.NotFound("target_plan_not_found", "Target plan was not found."));
        }

        var report = new ReportDto(
            Title: $"{SystemName} — Target GPA Plan",
            Tagline,
            DateTimeOffset.UtcNow,
            Baseline: new AcademicBaselineDto(plan.CurrentGpa, plan.CompletedCreditHours),
            Courses: plan.UpcomingCourses
                .Select(c => new GpaCourseResultDto(c.Name, c.CreditHours, GradeName: string.Empty, GpaPoints: 0m, QualityPoints: 0m))
                .ToList(),
            SemesterGpa: null,
            CumulativeGpa: null,
            TargetAnalysis: new TargetReportSectionDto(
                Round(plan.TargetGpa),
                Round(plan.RequiredAverageGpa),
                plan.IsAchievable,
                plan.MaxReachableGpa.HasValue ? Round(plan.MaxReachableGpa.Value) : null),
            GradeSuggestions: null);

        return Result<ReportDto>.Ok(report);
    }

    private static decimal Round(decimal value) => new GpaValue(value).Rounded;
}
