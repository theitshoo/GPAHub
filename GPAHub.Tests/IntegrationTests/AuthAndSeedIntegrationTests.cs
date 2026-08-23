using GPAHub.Application.Interfaces.Repositories;
using GPAHub.Domain.Constants;
using GPAHub.Domain.Entities;
using GPAHub.Infrastructure.Persistence;
using GPAHub.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

namespace GPAHub.Tests.IntegrationTests;

public class AuthAndSeedIntegrationTests : IClassFixture<LocalDbFixture>
{
    private readonly LocalDbFixture _fixture;

    public AuthAndSeedIntegrationTests(LocalDbFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task RegisterFlow_PersistsHashedStudent_AndDuplicateEmailIsDbGuarded()
    {
        await using var context = _fixture.CreateContext();
        IStudentRepository repository = new StudentRepository(context);

        var student = new Student("Seed User", "seeduser@test.com");
        student.SetPasswordHash("AQhashedvalue");
        await repository.AddAsync(student);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var found = await repository.GetByEmailAsync("SEEDUSER@test.com");
        Assert.NotNull(found);
        Assert.Equal("AQhashedvalue", found.PasswordHash);

        await repository.AddAsync(new Student("Other", "seeduser@test.com"));
        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }

    [Fact]
    public async Task DbSeeder_CreatesSystemDefaultScale_AndPlans_Idempotently()
    {
        await using var context = _fixture.CreateContext();

        await DbSeeder.SeedAsync(context);
        await DbSeeder.SeedAsync(context);

        var defaultScale = context.GradeScales.Single(s => s.StudentId == null);
        Assert.Equal(DbSeeder.SystemDefaultScaleName, defaultScale.Name);
        Assert.True(defaultScale.IsActive);
        Assert.Equal(5, defaultScale.Definitions.Count);
        Assert.Equal(4m, defaultScale.GetMaxGpaPoints());

        Assert.Equal(2, context.Plans.Count());
        Assert.Contains(context.Plans, p =>
            p.Name == Plan.PremiumName && p.HasFeature(FeatureFlags.GradeCombinations));
    }
}
