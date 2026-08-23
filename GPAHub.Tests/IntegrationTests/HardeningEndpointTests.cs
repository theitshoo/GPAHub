using System.Net;
using System.Text.Json;

namespace GPAHub.Tests.IntegrationTests;

[Collection("Api")]
public class HardeningEndpointTests : ApiTestBase
{
    public HardeningEndpointTests(GpaHubApiFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task Responses_CarrySecurityHeaders()
    {
        var response = await NewClient().GetAsync("/api/grade-scales/system-default");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("nosniff", response.Headers.GetValues("X-Content-Type-Options").Single());
        Assert.Equal("DENY", response.Headers.GetValues("X-Frame-Options").Single());
        Assert.Equal("no-referrer", response.Headers.GetValues("Referrer-Policy").Single());
    }

    [Fact]
    public async Task AuthEndpoints_RateLimit_ExcessRequestsGet429()
    {
        using var isolatedFactory = new GpaHubApiFactory();
        using var client = isolatedFactory.CreateClient();

        HttpStatusCode? lastStatus = null;
        var saw429 = false;

        for (var attempt = 1; attempt <= 35; attempt++)
        {
            var response = await client.PostAsync("/api/auth/login", Json(
                new { email = $"rl_{Guid.NewGuid():N}@test.com", password = "Whatever1" }));

            lastStatus = response.StatusCode;

            if (response.StatusCode == HttpStatusCode.TooManyRequests)
            {
                saw429 = true;
                break;
            }

            await Task.Delay(10);
        }

        Assert.True(saw429, $"Expected a 429 within 35 attempts; last status was {lastStatus}");
    }
}
