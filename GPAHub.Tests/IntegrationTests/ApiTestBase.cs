using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using GPAHub.Application.DTOs.Student;

namespace GPAHub.Tests.IntegrationTests;

[CollectionDefinition("Api")]
public class ApiTestCollection : ICollectionFixture<GpaHubApiFactory>
{
}

public abstract class ApiTestBase : IClassFixture<GpaHubApiFactory>
{
    protected readonly GpaHubApiFactory Factory;

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    protected ApiTestBase(GpaHubApiFactory factory)
    {
        Factory = factory;
    }

    protected HttpClient NewClient() => Factory.CreateClient();

    protected static StringContent Json<T>(T payload) =>
        new(JsonSerializer.Serialize(payload, JsonOptions), Encoding.UTF8, "application/json");

    protected async Task<(HttpClient Client, string Token, Guid StudentId, string Email)> CreateUserAsync(
        string? email = null)
    {
        var uniqueEmail = email ?? $"user_{Guid.NewGuid():N}@test.com";
        var client = NewClient();

        var response = await client.PostAsync("/api/auth/register", Json(
            new RegisterStudentDto("Test User", uniqueEmail, "Passw0rd!")));

        response.EnsureSuccessStatusCode();
        var auth = await response.Content.ReadFromJsonAsync<AuthResponseDto>(JsonOptions);

        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", auth!.AccessToken);

                var probe = await client.GetAsync("/api/students/profile");
        if (probe.StatusCode != System.Net.HttpStatusCode.OK)
        {
            var body = await probe.Content.ReadAsStringAsync();
            var hdr = string.Join("; ", probe.Headers.WwwAuthenticate.Select(h => h.Scheme + ":" + h.Parameter));
            throw new System.Exception($"PROBE: {(int)probe.StatusCode} | hdr=[{hdr}] | body=[{body}]");
        }
        return (client, auth!.AccessToken, auth.StudentId, uniqueEmail);
    }

    protected async Task<HttpClient> CreatePremiumUserAsync()
    {
        var (client, token, _, _) = await CreateUserAsync();

        var upgrade = await client.PostAsync("/api/subscription/upgrade", Json(
            new { amount = 9.99m, currency = "USD", externalReference = $"test-{Guid.NewGuid():N}" }));

        upgrade.EnsureSuccessStatusCode();

        return client;
    }

    protected static async Task<JsonElement> ReadProblemAsync(HttpResponseMessage response)
    {
        var content = await response.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(content);
        return document.RootElement.Clone();
    }
}


