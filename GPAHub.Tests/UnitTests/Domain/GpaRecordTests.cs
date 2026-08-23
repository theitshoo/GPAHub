using GPAHub.Domain.Entities;
using GPAHub.Domain.Enums;
using GPAHub.Domain.Exceptions;

namespace GPAHub.Tests.UnitTests.Domain;

public class GpaRecordTests
{
    private static readonly DateTimeOffset FixedTime = new(2026, 8, 23, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Constructor_WithValidData_CreatesRecord()
    {
        var record = new GpaRecord(
            Guid.NewGuid(), CalculationType.Gpa,
            semesterGpa: 3.25m, cumulativeGpa: 3.10m,
            totalCreditHours: 15m, totalQualityPoints: 48.75m,
            createdAtUtc: FixedTime);

        Assert.Equal(CalculationType.Gpa, record.CalculationType);
        Assert.Equal(3.25m, record.SemesterGpa);
        Assert.Equal(3.10m, record.CumulativeGpa);
        Assert.Equal(15m, record.TotalCreditHours);
        Assert.Equal(48.75m, record.TotalQualityPoints);
        Assert.Equal(FixedTime, record.CreatedAtUtc);
        Assert.Empty(record.CourseLines);
    }

    [Fact]
    public void Constructor_AllowsNullCumulative_WhenNoBaseline()
    {
        var record = new GpaRecord(Guid.NewGuid(), CalculationType.Gpa, 3.0m, null, 12m, 36m, FixedTime);

        Assert.Null(record.CumulativeGpa);
    }

    [Fact]
    public void Constructor_WithNegativeSemesterGpa_Throws()
    {
        Assert.Throws<DomainException>(
            () => new GpaRecord(Guid.NewGuid(), CalculationType.Gpa, -1m, null, 12m, 36m, FixedTime));
    }

    [Fact]
    public void Constructor_WithZeroTotalCreditHours_Throws()
    {
        Assert.Throws<DomainException>(
            () => new GpaRecord(Guid.NewGuid(), CalculationType.Gpa, 3.0m, null, 0m, 0m, FixedTime));
    }

    [Fact]
    public void AddLine_ComputesQualityPointsFromHoursAndPoints()
    {
        var record = CreateValidRecord();

        record.AddLine("Calculus", "MATH101", 3m, "A", 4.0m);

        var line = record.CourseLines.Single();
        Assert.Equal(12m, line.QualityPoints);
    }

    [Fact]
    public void AddLine_WithZeroHours_Throws()
    {
        var record = CreateValidRecord();

        Assert.Throws<DomainException>(() => record.AddLine("Calculus", null, 0m, "A", 4.0m));
    }

    [Fact]
    public void AddLine_WithEmptyName_Throws()
    {
        var record = CreateValidRecord();

        Assert.Throws<DomainException>(() => record.AddLine(" ", null, 3m, "A", 4.0m));
    }

    [Fact]
    public void AddLine_WithNegativePoints_Throws()
    {
        var record = CreateValidRecord();

        Assert.Throws<DomainException>(() => record.AddLine("Calculus", null, 3m, "A", -1m));
    }

    [Fact]
    public void CourseLines_ExposedAsReadOnly()
    {
        var record = CreateValidRecord();
        record.AddLine("Calculus", null, 3m, "A", 4.0m);

        var collection = (ICollection<GpaRecordCourseLine>)record.CourseLines;

        Assert.True(collection.IsReadOnly);
    }

    private static GpaRecord CreateValidRecord() =>
        new(Guid.NewGuid(), CalculationType.Gpa, 3.5m, null, 3m, 10.5m, FixedTime);
}
