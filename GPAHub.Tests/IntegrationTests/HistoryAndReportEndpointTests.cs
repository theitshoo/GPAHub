using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace GPAHub.Tests.IntegrationTests;

[Collection("Api")]
public class HistoryAndReportEndpointTests : ApiTestBase
{
    public HistoryAndReportEndpointTests(GpaHubApiFactory factory) : base(factory)
    {
    }

    private static StringContent JsonText(string body) => new(body, Encoding.UTF8, "application/json");

    private async Task<HttpClient> CreateUserWithSavedRecordsAsync(int recordCount)
    {
        var (client, _, _, _) = await CreateUserAsync();

        for (var i = 0; i < recordCount; i++)
        {
            var body = $$"""
                {"courses":[{"name":"Course {{i}}","creditHours":3,"inputType":"NumericMark","numericMark":{{90 - i}}}],
                 "customScaleDefinitions":[{"name":"A","minMark":0,"maxMark":100,"points":4}]}
                """;
            var response = await client.PostAsync("/api/gpa/calculate-and-save", JsonText(body));
            response.EnsureSuccessStatusCode();
        }

        return client;
    }

    [Fact]
    public async Task SavedCalculations_AppearInHistory_WithPagination()
    {
        var client = await CreateUserWithSavedRecordsAsync(3);

        var page1 = await client.GetFromJsonAsync<JsonElement>("/api/history/gpa-records?page=1&pageSize=2");

        Assert.Equal(3, page1.GetProperty("totalCount").GetInt32());
        Assert.Equal(2, page1.GetProperty("items").GetArrayLength());
        Assert.True(page1.GetProperty("hasNextPage").GetBoolean());
    }

    [Fact]
    public async Task GpaRecordDetail_And_Report_ContainExpectedContent()
    {
        var client = await CreateUserWithSavedRecordsAsync(1);

        var list = await client.GetFromJsonAsync<JsonElement>("/api/history/gpa-records?page=1&pageSize=5");
        var recordId = list.GetProperty("items")[0].GetProperty("id").GetString();

        var detail = await client.GetFromJsonAsync<JsonElement>($"/api/history/gpa-records/{recordId}");
        Assert.Equal("A", detail.GetProperty("courseLines")[0].GetProperty("gradeName").GetString());

        var report = await client.GetFromJsonAsync<JsonElement>($"/api/reports/gpa-records/{recordId}");
        Assert.Equal(
            "Your Academic Performance, All in One Place.",
            report.GetProperty("tagline").GetString());
        Assert.Equal(4m, report.GetProperty("semesterGpa").GetDecimal());
    }

    [Fact]
    public async Task DeleteGpaRecord_RemovesFromHistory()
    {
        var client = await CreateUserWithSavedRecordsAsync(2);

        var list = await client.GetFromJsonAsync<JsonElement>("/api/history/gpa-records?page=1&pageSize=5");
        var recordId = list.GetProperty("items")[0].GetProperty("id").GetString()!;

        var delete = await client.DeleteAsync($"/api/history/gpa-records/{recordId}");
        Assert.Equal(HttpStatusCode.OK, delete.StatusCode);

        var after = await client.GetFromJsonAsync<JsonElement>("/api/history/gpa-records?page=1&pageSize=5");
        Assert.Equal(1, after.GetProperty("totalCount").GetInt32());

        var deletedFetch = await client.GetAsync($"/api/history/gpa-records/{recordId}");
        Assert.Equal(HttpStatusCode.NotFound, deletedFetch.StatusCode);
    }

    [Fact]
    public async Task BaselineFlow_StoredBaseline_IsUsedByCalculateForMe()
    {
        var (client, _, _, _) = await CreateUserAsync();

        var baseline = await client.PutAsync("/api/students/baseline", Json(
            new { currentGpa = 2.0m, completedCreditHours = 30m }));
        baseline.EnsureSuccessStatusCode();

        var body = """
            {"courses":[{"name":"Only","creditHours":10,"inputType":"NumericMark","numericMark":50}],
             "customScaleDefinitions":[{"name":"P","minMark":0,"maxMark":100,"points":2}]}
            """;
        var result = await client.PostAsync("/api/gpa/calculate-for-me", JsonText(body));

        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        var json = await result.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(2.0m, json.GetProperty("cumulativeGpa").GetDecimal());
    }
}

