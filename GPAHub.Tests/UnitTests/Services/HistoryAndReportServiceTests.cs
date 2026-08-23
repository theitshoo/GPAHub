using AutoMapper;
using GPAHub.Application.Common;
using GPAHub.Application.DTOs.History;
using GPAHub.Application.DTOs.Report;
using GPAHub.Application.Interfaces.Repositories;
using GPAHub.Application.Interfaces.Services;
using GPAHub.Application.Mappings;
using GPAHub.Application.Services;
using GPAHub.Domain.Entities;
using GPAHub.Domain.Enums;
using Moq;

namespace GPAHub.Tests.UnitTests.Services;

public class HistoryAndReportServiceTests
{
    private static readonly DateTimeOffset FixedTime = new(2026, 8, 23, 10, 0, 0, TimeSpan.Zero);

    private readonly Mock<IGpaRecordRepository> _recordRepo = new();
    private readonly Mock<ITargetPlanRepository> _planRepo = new();
    private readonly Mock<IStudentRepository> _studentRepo = new();
    private readonly HistoryService _history;
    private readonly ReportService _report;
    private readonly Student _student = new("Ali", "ali@test.com");

    public HistoryAndReportServiceTests()
    {
        var mapper = new MapperConfiguration(cfg => cfg.AddProfile<MappingProfile>()).CreateMapper();
        _history = new HistoryService(_recordRepo.Object, _planRepo.Object, _uow.Object, mapper);
        _report = new ReportService(_recordRepo.Object, _planRepo.Object, _studentRepo.Object);
        _student.UpdateBaseline(3m, 40m);
        _studentRepo.Setup(r => r.GetByIdAsync(_student.Id, It.IsAny<CancellationToken>())).ReturnsAsync(_student);
    }

    private readonly Mock<IUnitOfWork> _uow = new();

    [Fact]
    public async Task ListGpaRecords_PagesAndMaps()
    {
        var record = BuildRecord();
        _recordRepo.Setup(r => r.ListByStudentAsync(_student.Id, 2, 5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(([record], 11));

        var result = await _history.ListGpaRecordsAsync(_student.Id, new HistoryPageRequest(2, 5));

        Assert.True(result.IsSuccess);
        Assert.Equal(11, result.Value.TotalCount);
        Assert.Single(result.Value.Items);
        Assert.Equal(3.0m, result.Value.Items[0].SemesterGpa);
        Assert.True(result.Value.HasNextPage);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-3)]
    public async Task ListGpaRecords_ClampsInvalidPage(int page)
    {
        _recordRepo.Setup(r => r.ListByStudentAsync(_student.Id, 1, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Array.Empty<GpaRecord>(), 0));

        await _history.ListGpaRecordsAsync(_student.Id, new HistoryPageRequest(page, 10));

        _recordRepo.Verify(r => r.ListByStudentAsync(_student.Id, 1, 10, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetGpaRecord_AnotherStudentsRecord_ReturnsNotFound()
    {
        _recordRepo.Setup(r => r.GetByIdForStudentAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((GpaRecord?)null);

        var result = await _history.GetGpaRecordAsync(Guid.NewGuid(), Guid.NewGuid());

        Assert.Equal(ErrorType.NotFound, result.Error!.Type);
    }

    [Fact]
    public async Task DeleteTargetPlan_RemovesOwnedPlan()
    {
        var plan = BuildPlan();
        _planRepo.Setup(r => r.GetByIdForStudentAsync(plan.Id, _student.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(plan);

        var result = await _history.DeleteTargetPlanAsync(_student.Id, plan.Id);

        Assert.True(result.IsSuccess);
        _planRepo.Verify(r => r.Remove(plan), Times.Once);
    }

    [Fact]
    public async Task GpaReport_ContainsBrandingBaselineAndCourses()
    {
        var record = BuildRecord();
        _recordRepo.Setup(r => r.GetByIdForStudentAsync(record.Id, _student.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(record);

        var result = await _report.BuildGpaReportAsync(_student.Id, record.Id);

        Assert.True(result.IsSuccess);
        Assert.Equal("Your Academic Performance, All in One Place.", result.Value.Tagline);
        Assert.Equal(3m, result.Value.Baseline!.CurrentGpa);
        Assert.Equal(40m, result.Value.Baseline.CompletedCreditHours);
        Assert.Equal(3.0m, result.Value.SemesterGpa);
        Assert.Equal("Calculus", result.Value.Courses.Single().Name);
        Assert.Null(result.Value.TargetAnalysis);
    }

    [Fact]
    public async Task TargetReport_ContainsAnalysisSection()
    {
        var plan = BuildPlan();
        _planRepo.Setup(r => r.GetByIdForStudentAsync(plan.Id, _student.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(plan);

        var result = await _report.BuildTargetReportAsync(_student.Id, plan.Id);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value.TargetAnalysis);
        Assert.True(result.Value.TargetAnalysis!.IsAchievable);
        Assert.Equal(3.5m, result.Value.TargetAnalysis.TargetGpa);
        Assert.Equal(2, result.Value.Courses.Count);
    }

    private GpaRecord BuildRecord()
    {
        var record = new GpaRecord(_student.Id, CalculationType.Gpa, 3.0m, 3.05m, 6m, 18m, FixedTime);
        record.AddLine("Calculus", "MATH101", 3m, "B", 3m);
        return record;
    }

    private TargetPlan BuildPlan()
    {
        var plan = new TargetPlan(_student.Id, 3.5m, 3.2m, 60m, 3.8m, true, 4m, FixedTime);
        plan.AddUpcomingCourse("Alpha", 3m);
        plan.AddUpcomingCourse("Beta", 4m);
        return plan;
    }
}
