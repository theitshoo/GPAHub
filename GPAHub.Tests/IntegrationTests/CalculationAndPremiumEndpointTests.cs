using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json.Serialization;
using System.Text.Json;

namespace GPAHub.Tests.IntegrationTests;

[Collection("Api")]
public class CalculationAndPremiumEndpointTests : ApiTestBase
{
    public CalculationAndPremiumEndpointTests(GpaHubApiFactory factory) : base(factory)
    {
    }

    private const string GuestCalcBody = """
        {
          "courses": [
            {"name":"Math","creditHours":3,"inputType":"NumericMark","numericMark":90},
            {"name":"Art","creditHours":3,"inputType":"LetterGrade","letterGrade":"B"}
          ],
          "customScaleDefinitions": [
            {"name":"A","minMark":85,"maxMark":100,"points":4},
            {"name":"B","minMark":70,"maxMark":84,"points":3}
          ]
        }
        """;

    private static StringContent JsonText(string body) => new(body, Encoding.UTF8, "application/json");

    [Fact]
    public async Task AnonymousGuest_Calculation_Works_WithoutAccount()
    {
        var response = await NewClient().PostAsync("/api/gpa/calculate", JsonText(GuestCalcBody));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(3.5m, json.GetProperty("semesterGpa").GetDecimal());
        Assert.Equal(6m, json.GetProperty("totalCreditHours").GetDecimal());
    }

    [Fact]
    public async Task Guest_RequestingCombinations_IsForbidden403()
    {
        var body = """
            {"currentGpa":0,"completedCreditHours":0,"targetGpa":3.0,
             "upcomingCourses":[{"name":"A","creditHours":3}],"includeCombinations":true}
            """;

        var response = await NewClient().PostAsync("/api/target/predict", JsonText(body));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        var problem = await ReadProblemAsync(response);
        Assert.Equal("premium_required", problem.GetProperty("code").GetString());
    }

    [Fact]
    public async Task FreeAuthenticatedUser_RequestingCombinations_IsForbidden403()
    {
        var (client, _, _, _) = await CreateUserAsync();

        var body = """
            {"currentGpa":0,"completedCreditHours":0,"targetGpa":3.0,
             "upcomingCourses":[{"name":"A","creditHours":3}],"includeCombinations":true}
            """;

        var response = await client.PostAsync("/api/target/predict", JsonText(body));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task PremiumUser_GetsCombos_OrderedClosestFirst()
    {
        var client = await CreatePremiumUserAsync();

        var body = """
            {"currentGpa":0,"completedCreditHours":0,"targetGpa":3.5,
             "upcomingCourses":[{"name":"Math","creditHours":3},{"name":"Art","creditHours":3}],
             "includeCombinations":true}
            """;

        var response = await client.PostAsync("/api/target/predict", JsonText(body));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        var combos = json.GetProperty("combinations");
        Assert.True(combos.GetArrayLength() > 0);

        var gpas = combos.EnumerateArray()
            .Select(c => c.GetProperty("resultingGpa").GetDecimal())
            .ToList();
        Assert.Equal(gpas.OrderBy(g => g), gpas);
        Assert.All(gpas, g => Assert.True(g >= 3.5m));
    }

    [Fact]
    public async Task InfeasibleTarget_ReturnsMaxReachableGpa()
    {
        var (client, _, _, _) = await CreateUserAsync();

        var body = """
            {"currentGpa":3.2,"completedCreditHours":60,"targetGpa":3.9,
             "upcomingCourses":[{"name":"OnlyCourse","creditHours":15}]}
            """;

        var response = await client.PostAsync("/api/target/predict", JsonText(body));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.False(json.GetProperty("isAchievable").GetBoolean());
        Assert.Equal(6.7m, json.GetProperty("requiredAverageGpa").GetDecimal());
        Assert.Equal(3.36m, json.GetProperty("maxReachableGpa").GetDecimal());
    }
}

