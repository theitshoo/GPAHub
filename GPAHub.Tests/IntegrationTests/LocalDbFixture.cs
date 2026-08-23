using GPAHub.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GPAHub.Tests.IntegrationTests;

public sealed class LocalDbFixture : IAsyncLifetime
{
    private readonly string _databaseName = $"GPAHub_Tests_{Guid.NewGuid():N}";

    public string ConnectionString =>
        $@"Server=(localdb)\MSSQLLocalDB;Database={_databaseName};Trusted_Connection=True;TrustServerCertificate=True";

    private DbContextOptions<GpaHubDbContext> Options { get; set; } = null!;

    public GpaHubDbContext CreateContext() => new(Options);

    public async Task InitializeAsync()
    {
        Options = new DbContextOptionsBuilder<GpaHubDbContext>()
            .UseSqlServer(ConnectionString)
            .Options;

        await using var context = CreateContext();
        await context.Database.EnsureCreatedAsync();
    }

    public async Task DisposeAsync()
    {
        await using var context = CreateContext();
        await context.Database.EnsureDeletedAsync();
    }
}
