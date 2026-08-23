using GPAHub.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace GPAHub.Tests.IntegrationTests;

public sealed class GpaHubApiFactory : WebApplicationFactory<Program>
{
    private readonly string _databaseName = $"GPAHub_ApiTests_{Guid.NewGuid():N}";

    public const string StripeWebhookSecret = "whsec_test_secret_for_integration_tests";

    public GpaHubDbContext CreateDbContext()
    {
        var scope = Services.CreateAsyncScope();
        return scope.ServiceProvider.GetRequiredService<GpaHubDbContext>();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("ConnectionStrings:DefaultConnection",
            $@"Server=(localdb)\MSSQLLocalDB;Database={_databaseName};Trusted_Connection=True;TrustServerCertificate=True");

        builder.UseSetting("Jwt:SecretKey", "test-secret-key-for-api-tests-only-0123456789abcdef");
        builder.UseSetting("Cors:AllowedOrigins", "http://localhost:3000");
        builder.UseSetting("Stripe:WebhookSecret", StripeWebhookSecret);
    }
}
