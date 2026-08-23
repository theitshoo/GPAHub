using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace GPAHub.Tests.IntegrationTests;

[Collection("Api")]
public class SemesterAndPdfEndpointTests : ApiTestBase
{
    public SemesterAndPdfEndpointTests(GpaHubApiFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task SemesterJourney_CreateListRenameDelete()
    {
        var (client, _, _, _) = await CreateUserAsync();

        var created = await client.PostAsJsonAsync("/api/semesters", new { name = "Fall 2026" });
        Assert.Equal(HttpStatusCode.OK, created.StatusCode);
        var semester = await created.Content.ReadFromJsonAsync<JsonElement>();
        var semesterId = semester.GetProperty("id").GetString()!;

        // attach a course so deletion must detach it (DR-014)
        var course = await (await client.PostAsJsonAsync("/api/courses", new
        {
            name = "Attached Course",
            code = (string?)null,
            creditHours = 3m,
            inputType = "NumericMark",
            numericMark = 88,
            semesterId
        })).Content.ReadFromJsonAsync<JsonElement>();

        var list = await client.GetFromJsonAsync<JsonElement>("/api/semesters");
        Assert.Equal(1, list.GetArrayLength());

        var rename = await client.PutAsJsonAsync($"/api/semesters/{semesterId}", new { name = "Autumn 2026" });
        Assert.Equal(HttpStatusCode.OK, rename.StatusCode);

        var deleted = await client.DeleteAsync($"/api/semesters/{semesterId}");
        Assert.Equal(HttpStatusCode.OK, deleted.StatusCode);

        var detachedCourse = await client.GetFromJsonAsync<JsonElement>($"/api/courses/{course.GetProperty("id").GetString()}");
        Assert.Equal(JsonValueKind.Null, detachedCourse.GetProperty("semesterId").ValueKind);
    }

    private static StringContent JsonText(string body) => new(body, Encoding.UTF8, "application/json");

    [Fact]
    public async Task GpaReportPdf_ReturnsRealPdfDocument()
    {
        var client = NewClient();

        var register = await client.PostAsync("/api/auth/register", Json(
            new GPAHub.Application.DTOs.Student.RegisterStudentDto("Pdf User", $"pdf_{Guid.NewGuid():N}@test.com", "Passw0rd!")));
        register.EnsureSuccessStatusCode();
        var auth = await register.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
        var pdfClient = NewClient();
        pdfClient.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", auth.GetProperty("accessToken").GetString());

        var body = """
            {"courses":[{"name":"Calculus","creditHours":3,"inputType":"NumericMark","numericMark":92}],
             "customScaleDefinitions":[{"name":"A","minMark":80,"maxMark":100,"points":4}]}
            """;
        var saved = await pdfClient.PostAsync("/api/gpa/calculate-and-save", JsonText(body));
        saved.EnsureSuccessStatusCode();

        var list = await pdfClient.GetFromJsonAsync<JsonElement>("/api/history/gpa-records?page=1&pageSize=5");
        var recordId = list.GetProperty("items")[0].GetProperty("id").GetString();

        var pdfResponse = await pdfClient.GetAsync($"/api/reports/gpa-records/{recordId}/pdf");

        Assert.Equal(HttpStatusCode.OK, pdfResponse.StatusCode);
        Assert.Equal("application/pdf",
            pdfResponse.Content.Headers.ContentType?.MediaType ?? "application/pdf");
        var bytes = await pdfResponse.Content.ReadAsByteArrayAsync();
        Assert.True(bytes.Length > 500);
        Assert.Equal((byte)'%', bytes[0]);
        Assert.Equal((byte)'P', bytes[1]);
        Assert.Equal((byte)'D', bytes[2]);
        Assert.Equal((byte)'F', bytes[3]);
    }

    [Fact]
    public async Task PdfReport_OfAnotherStudentsRecord_IsNotFound()
    {
        var (clientA, _, _, _) = await CreateUserAsync();

        var body = """
            {"courses":[{"name":"Private","creditHours":2,"inputType":"NumericMark","numericMark":95}],
             "customScaleDefinitions":[{"name":"A","minMark":0,"maxMark":100,"points":4}]}
            """;
        var saved = await clientA.PostAsync("/api/gpa/calculate-and-save", JsonText(body));
        saved.EnsureSuccessStatusCode();

        var list = await clientA.GetFromJsonAsync<JsonElement>("/api/history/gpa-records?page=1&pageSize=5");
        var recordId = list.GetProperty("items")[0].GetProperty("id").GetString();

        var attacker = await CreateUserAsync();
        var attackerClient = attacker.Client;

        var response = await attackerClient.GetAsync($"/api/reports/gpa-records/{recordId}/pdf");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
