using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace GPAHub.Tests.IntegrationTests;

[Collection("Api")]
public class OwnershipAndScaleEndpointTests : ApiTestBase
{
    public OwnershipAndScaleEndpointTests(GpaHubApiFactory factory) : base(factory)
    {
    }

    private static StringContent JsonText(string body) => new(body, Encoding.UTF8, "application/json");

    [Fact]
    public async Task Course_CannotBeAccessedByAnotherStudent_IdorGuard()
    {
        var (ownerClient, _, ownerId, _) = await CreateUserAsync();
        var (attackerClient, _, attackerId, _) = await CreateUserAsync();

        var created = await (await ownerClient.PostAsync("/api/courses", Json(
            new { name = "Secret Course", code = (string?)null, creditHours = 3m,
                  inputType = "NumericMark", numericMark = 85 })))
            .Content.ReadFromJsonAsync<JsonElement>();
        var courseId = created.GetProperty("id").GetString();

        var ownerView = await ownerClient.GetAsync($"/api/courses/{courseId}");
        if (ownerView.StatusCode != HttpStatusCode.OK) { var b = await ownerView.Content.ReadAsStringAsync(); throw new System.Exception("DEBUG401: " + ownerView.StatusCode + " | " + b.Substring(0, System.Math.Min(300, b.Length)) + " | hdr=" + string.Join(";", ownerView.Headers.WwwAuthenticate.Select(h => h.Scheme + ":" + h.Parameter))); }
        var attackerView = await attackerClient.GetAsync($"/api/courses/{courseId}");

        Assert.Equal(HttpStatusCode.OK, ownerView.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, attackerView.StatusCode);
    }

    [Fact]
    public async Task SavedGpaRecord_CannotBeReadByAnotherStudent()
    {
        var (clientA, _, _, _) = await CreateUserAsync();
        var (clientB, _, _, _) = await CreateUserAsync();

        var body = """
            {"courses":[{"name":"X","creditHours":2,"inputType":"NumericMark","numericMark":95}],
             "customScaleDefinitions":[{"name":"A","minMark":0,"maxMark":100,"points":4}]}
            """;
        var saveResponse = await clientA.PostAsync("/api/gpa/calculate-and-save", JsonText(body));
        saveResponse.EnsureSuccessStatusCode();

        var listForB = await clientB.GetAsync("/api/history/gpa-records");
        Assert.Equal(HttpStatusCode.OK, listForB.StatusCode);
        var pageForB = await listForB.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(0, pageForB.GetProperty("totalCount").GetInt32());
    }

    [Fact]
    public async Task GradeScaleJourney_CreateAddActivate_ThenIncompleteActivationFails()
    {
        var (client, _, _, _) = await CreateUserAsync();

        var created = await client.PostAsync("/api/grade-scales", Json(
            new { name = "My Scale", description = "journey", enforceFullCoverage = false }));
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var scale = await created.Content.ReadFromJsonAsync<JsonElement>();
        var scaleId = scale.GetProperty("id").GetString();

        var addDef = await client.PostAsync($"/api/grade-scales/{scaleId}/definitions", Json(
            new { name = "A", minMark = 80, maxMark = 100, points = 4m }));
        Assert.Equal(HttpStatusCode.OK, addDef.StatusCode);

        var activate = await client.PostAsync($"/api/grade-scales/{scaleId}/activate", null);
        Assert.Equal(HttpStatusCode.OK, activate.StatusCode);

        var incomplete = new GradeScaleRef(await client.PostAsync("/api/grade-scales", Json(
            new { name = "Empty Scale", description = "", enforceFullCoverage = false })));
        var emptyId = incomplete.Id;
        var failedActivation = await client.PostAsync($"/api/grade-scales/{emptyId}/activate", null);
        Assert.Equal(HttpStatusCode.BadRequest, failedActivation.StatusCode);
        var problem = await ReadProblemAsync(failedActivation);
        Assert.Equal("scale_not_ready", problem.GetProperty("code").GetString());
    }

    [Fact]
    public async Task OverlappingDefinition_Returns409Conflict()
    {
        var (client, _, _, _) = await CreateUserAsync();

        var created = await client.PostAsync("/api/grade-scales", Json(
            new { name = $"Overlap_{Guid.NewGuid():N}", description = "", enforceFullCoverage = false }));
        var scaleId = (await created.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetString();

        await client.PostAsync($"/api/grade-scales/{scaleId}/definitions", Json(
            new { name = "A", minMark = 90, maxMark = 100, points = 4m }));

        var conflict = await client.PostAsync($"/api/grade-scales/{scaleId}/definitions", Json(
            new { name = "B", minMark = 85, maxMark = 95, points = 3m }));

        Assert.Equal(HttpStatusCode.Conflict, conflict.StatusCode);
    }

    private sealed record GradeScaleRef(HttpResponseMessage Response)
    {
        public string Id => Response.Content.ReadFromJsonAsync<JsonElement>()
            .GetAwaiter().GetResult().GetProperty("id").GetString()!;
    }
}



