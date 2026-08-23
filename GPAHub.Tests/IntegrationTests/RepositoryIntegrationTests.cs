using GPAHub.Application.Interfaces.Repositories;
using GPAHub.Domain.Entities;
using GPAHub.Domain.Enums;
using GPAHub.Infrastructure.Persistence;
using GPAHub.Infrastructure.Repositories;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace GPAHub.Tests.IntegrationTests;

[CollectionDefinition("LocalDb")]
public class LocalDbCollection : ICollectionFixture<LocalDbCollection> { }

public class RepositoryIntegrationTests : IClassFixture<LocalDbFixture>
{
    private readonly LocalDbFixture _fixture;

    public RepositoryIntegrationTests(LocalDbFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task StudentRepository_RoundTripsAndNormalizesEmailLookup()
    {
        await using var context = _fixture.CreateContext();
        var repository = new StudentRepository(context);

        var student = new Student("Ali", "Ali@Test.COM");
        await repository.AddAsync(student);
        await context.SaveChangesAsync();

        var found = await repository.GetByEmailAsync("ali@test.com");
        Assert.NotNull(found);
        Assert.Equal(student.Id, found.Id);
    }

    [Fact]
    public async Task UniqueEmailIndex_RejectsDuplicatesAtDatabaseLevel()
    {
        await using var context = _fixture.CreateContext();
        var repository = new StudentRepository(context);

        await repository.AddAsync(new Student("First", "dup@test.com"));
        await context.SaveChangesAsync();

        await repository.AddAsync(new Student("Second", "DUP@test.com"));

        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }

    [Fact]
    public async Task GradeScaleRepository_OwnershipFilter_HidesOtherStudentsScales()
    {
        await using var context = _fixture.CreateContext();
        IGradeScaleRepository repository = new GradeScaleRepository(context);

        var owner = new Student("Owner", "owner@test.com");
        var scale = new GradeScale("Private Scale", owner.Id);
        scale.AddDefinition("A", 90, 100, 4m);
        await context.Students.AddAsync(owner);
        await repository.AddAsync(scale);
        await context.SaveChangesAsync();

        Assert.Null(await repository.GetByIdForStudentAsync(scale.Id, Guid.NewGuid()));
        Assert.NotNull(await repository.GetByIdForStudentAsync(scale.Id, owner.Id));
    }

    [Fact]
    public async Task ActiveScaleQuery_AndSystemDefault_ResolveCorrectly()
    {
        await using var context = _fixture.CreateContext();
        IGradeScaleRepository repository = new GradeScaleRepository(context);

        var systemDefault = new GradeScale("System Default", null, enforceFullCoverage: false);
        systemDefault.AddDefinition("P", 0, 100, 4m);
        systemDefault.Activate();
        await repository.AddAsync(systemDefault);

        var student = new Student("S", "s@t.com");
        var inactive = new GradeScale("Inactive", student.Id);
        inactive.AddDefinition("A", 0, 50, 3m);
        var active = new GradeScale("Active", student.Id);
        active.AddDefinition("B", 0, 60, 2m);
        active.Activate();
        await context.Students.AddAsync(student);
        await repository.AddAsync(inactive);
        await repository.AddAsync(active);
        await context.SaveChangesAsync();

        var resolvedActive = await repository.GetActiveForStudentAsync(student.Id);
        Assert.Equal("Active", resolvedActive!.Name);
        Assert.Single(resolvedActive.Definitions);

        var resolvedDefault = await repository.GetSystemDefaultAsync();
        Assert.Equal(systemDefault.Id, resolvedDefault!.Id);
    }

    [Fact]
    public async Task SemesterDeletion_TrackedFlowDetaches_WhileRawDeleteIsDbGuarded()
    {
        await using var context = _fixture.CreateContext();
        ICourseRepository courseRepository = new CourseRepository(context);
        ISemesterRepository semesterRepository = new SemesterRepository(context);

        var student = new Student("C", "c@t.com");
        var semester = new Semester(student.Id, "Fall");
        var course = Course.CreateNumeric(student.Id, "Math", null, 3m, 80);
        course.AssignToSemester(semester.Id);
        await context.Students.AddAsync(student);
        await semesterRepository.AddAsync(semester);
        await courseRepository.AddAsync(course);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var sqlException = await Assert.ThrowsAnyAsync<SqlException>(async () =>
            await context.Database.ExecuteSqlRawAsync("DELETE FROM Semesters WHERE Id = {0}", semester.Id));
        Assert.NotNull(sqlException);

        var trackedCourse = await courseRepository.GetByIdForStudentAsync(course.Id, student.Id);
        var trackedSemester = await semesterRepository.GetByIdForStudentAsync(semester.Id, student.Id);

        semesterRepository.Remove(trackedSemester!);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var reloaded = await courseRepository.GetByIdForStudentAsync(course.Id, student.Id);
        Assert.NotNull(reloaded);
        Assert.Null(reloaded.SemesterId);
    }

    [Fact]
    public async Task CreditHoursValueObject_PersistsAndReloads()
    {
        await using var context = _fixture.CreateContext();
        ICourseRepository repository = new CourseRepository(context);

        var student = new Student("V", "v@t.com");
        var course = Course.CreateNumeric(student.Id, "Fractions", null, 1.5m, 90);
        await context.Students.AddAsync(student);
        await repository.AddAsync(course);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var reloaded = await repository.GetByIdForStudentAsync(course.Id, student.Id);
        Assert.Equal(1.5m, reloaded!.CreditHours.Value);
    }

    [Fact]
    public async Task GpaRecord_ChildLines_PersistWithComputedQualityPoints()
    {
        await using var context = _fixture.CreateContext();
        IGpaRecordRepository repository = new GpaRecordRepository(context);

        var student = new Student("H", "h@t.com");
        var record = new GpaRecord(student.Id, CalculationType.Gpa, 4.0m, null, 3m, 12m, DateTimeOffset.UtcNow);
        record.AddLine("Calculus", "MATH101", 3m, "A", 4m);
        await context.Students.AddAsync(student);
        await repository.AddAsync(record);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var loaded = await repository.GetByIdForStudentAsync(record.Id, student.Id);
        Assert.Single(loaded!.CourseLines);
        Assert.Equal(12m, loaded.CourseLines[0].QualityPoints);
        Assert.Equal(record.Id, loaded.CourseLines[0].GpaRecordId);
    }

    [Fact]
    public async Task HistoryPagination_ReturnsStableTotalCount()
    {
        await using var context = _fixture.CreateContext();
        IGpaRecordRepository repository = new GpaRecordRepository(context);

        var student = new Student("P", "p@t.com");
        await context.Students.AddAsync(student);
        for (var i = 0; i < 7; i++)
        {
            var record = new GpaRecord(student.Id, CalculationType.Gpa, 2m + i / 10m, null, 1m, 1m,
                DateTimeOffset.UtcNow.AddMinutes(i));
            await repository.AddAsync(record);
        }
        await context.SaveChangesAsync();

        var (firstPage, totalCount) = await repository.ListByStudentAsync(student.Id, page: 1, pageSize: 5);
        Assert.Equal(5, firstPage.Count);
        Assert.Equal(7, totalCount);

        var (secondPage, _) = await repository.ListByStudentAsync(student.Id, page: 2, pageSize: 5);
        Assert.Equal(2, secondPage.Count);
    }

    [Fact]
    public async Task SubscriptionPayments_LoadedViaInclude()
    {
        await using var context = _fixture.CreateContext();
        ISubscriptionRepository repository = new SubscriptionRepository(context);

        var student = new Student("U", "u@t.com");
        var subscription = new Subscription(student.Id, SubscriptionType.Premium, DateTimeOffset.UtcNow, null);
        var payment = subscription.AddPayment(9.99m, "USD", DateTimeOffset.UtcNow, "txn-1");
        payment.MarkCompleted();
        await context.Students.AddAsync(student);
        await repository.AddAsync(subscription);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var loaded = await repository.GetLatestForStudentAsync(student.Id);
        Assert.Single(loaded!.Payments);
        Assert.Equal(PaymentStatus.Completed, loaded.Payments[0].Status);
    }

    [Fact]
    public async Task DatabaseCheckConstraint_BlocksInvalidPoints_IndependentOfDomain()
    {
        await using var context = _fixture.CreateContext();

        var student = new Student("X", "x@t.com");
        var scale = new GradeScale("Scale", student.Id);
        scale.AddDefinition("A", 90, 100, 4m);
        await context.Students.AddAsync(student);
        await context.GradeScales.AddAsync(scale);
        await context.SaveChangesAsync();

        var sql = "INSERT INTO GradeDefinitions (Id, GradeScaleId, Name, MinMark, MaxMark, Points) VALUES ({0}, {1}, 'BAD', 10, 20, {2})";
        await Assert.ThrowsAsync<SqlException>(async () =>
            await context.Database.ExecuteSqlRawAsync(sql, Guid.NewGuid(), scale.Id, -1m));
    }
}
