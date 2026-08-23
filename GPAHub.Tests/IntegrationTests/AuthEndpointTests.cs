using System.Net;
using System.Net.Http.Json;
using GPAHub.Application.DTOs.Student;

namespace GPAHub.Tests.IntegrationTests;

[Collection("Api")]
public class AuthEndpointTests : ApiTestBase
{
    public AuthEndpointTests(GpaHubApiFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task Register_ThenLogin_ReturnsTokens()
    {
        var client = NewClient();
        var email = $"auth_{Guid.NewGuid():N}@test.com";

        var register = await client.PostAsync("/api/auth/register", Json(
            new RegisterStudentDto("Auth Tester", email, "Passw0rd!")));
        Assert.Equal(HttpStatusCode.OK, register.StatusCode);

        var login = await client.PostAsync("/api/auth/login", Json(
            new LoginRequestDto(email.ToUpperInvariant(), "Passw0rd!")));
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        Assert.NotEmpty((await login.Content.ReadFromJsonAsync<AuthResponseDto>()).AccessToken);
    }

    [Fact]
    public async Task Register_DuplicateEmail_Returns409WithEmailTakenCode()
    {
        var (client, _, _, email) = await CreateUserAsync();

        var duplicate = await client.PostAsync("/api/auth/register", Json(
            new RegisterStudentDto("Other", email, "Passw0rd!")));

        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);
        var problem = await ReadProblemAsync(duplicate);
        Assert.Equal("email_taken", problem.GetProperty("code").GetString());
    }

    [Fact]
    public async Task Login_UnknownEmail_AndWrongPassword_AreIndistinguishable()
    {
        var (_, token, _, email) = await CreateUserAsync();
        var client = NewClient();

        var unknown = await client.PostAsync("/api/auth/login", Json(
            new LoginRequestDto($"nope_{Guid.NewGuid():N}@test.com", "Whatever1")));
        var wrongPassword = await client.PostAsync("/api/auth/login", Json(
            new LoginRequestDto(email, "WrongPass9")));

        Assert.Equal(HttpStatusCode.Unauthorized, unknown.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, wrongPassword.StatusCode);

        var unknownProblem = await ReadProblemAsync(unknown);
        var wrongProblem = await ReadProblemAsync(wrongPassword);
        Assert.Equal(unknownProblem.GetProperty("code").GetString(), wrongProblem.GetProperty("code").GetString());
        Assert.Equal(unknownProblem.GetProperty("title").GetString(), wrongProblem.GetProperty("title").GetString());
    }

    [Fact]
    public async Task ProtectedEndpoint_WithoutToken_Returns401()
    {
        var response = await NewClient().GetAsync("/api/students/profile");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Register_WeakPassword_Returns400WithStableCode()
    {
        var response = await NewClient().PostAsync("/api/auth/register", Json(
            new RegisterStudentDto("Someone", $"weak_{Guid.NewGuid():N}@test.com", "short1")));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await ReadProblemAsync(response);
        Assert.Equal("password_too_short", problem.GetProperty("code").GetString());
    }
}
