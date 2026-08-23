using System.Net;
using System.Net.Http.Json;
using System.Text;
using GPAHub.Domain.Entities;
using GPAHub.Domain.Exceptions;
using GPAHub.Tests.IntegrationTests;

namespace GPAHub.Tests.UnitTests.Services;

[Collection("Api")]
public class RefreshTokenRotationTests : ApiTestBase
{
    public RefreshTokenRotationTests(GpaHubApiFactory factory) : base(factory)
    {
    }

    private static StringContent JsonText(string body) => new(body, Encoding.UTF8, "application/json");

    [Fact]
    public async Task Register_ReturnsBothTokens_AndRefreshRotates()
    {
        var client = NewClient();
        var email = $"rot_{Guid.NewGuid():N}@test.com";

        var register = await client.PostAsync("/api/auth/register", Json(
            new GPAHub.Application.DTOs.Student.RegisterStudentDto("Rot User", email, "Passw0rd!")));
        register.EnsureSuccessStatusCode();

        var auth = await register.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
        var refreshToken = auth.GetProperty("refreshToken").GetString()!;
        Assert.False(string.IsNullOrWhiteSpace(refreshToken));

        var refreshResponse = await client.PostAsync("/api/auth/refresh", JsonText(
            $$"""{"refreshToken":"{{refreshToken}}"}"""));

        Assert.Equal(HttpStatusCode.OK, refreshResponse.StatusCode);
        var rotated = await refreshResponse.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
        Assert.NotEqual(refreshToken, rotated.GetProperty("refreshToken").GetString());
        Assert.False(string.IsNullOrWhiteSpace(rotated.GetProperty("accessToken").GetString()));

        var reusedOld = await client.PostAsync("/api/auth/refresh", JsonText(
            $$"""{"refreshToken":"{{refreshToken}}"}"""));
        Assert.Equal(HttpStatusCode.Unauthorized, reusedOld.StatusCode);
    }

    [Fact]
    public async Task RefreshTokenReuse_RevokesEntireFamily()
    {
        var client = NewClient();

        var register = await client.PostAsync("/api/auth/register", Json(
            new GPAHub.Application.DTOs.Student.RegisterStudentDto("Family User", $"fam_{Guid.NewGuid():N}@test.com", "Passw0rd!")));
        var first = await register.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
        var token1 = first.GetProperty("refreshToken").GetString()!;

        var second = await client.PostAsync("/api/auth/refresh", JsonText(
            $$"""{"refreshToken":"{{token1}}"}"""));
        var secondAuth = await second.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
        var token2 = secondAuth.GetProperty("refreshToken").GetString()!;

        var stolenUse = await client.PostAsync("/api/auth/refresh", JsonText(
            $$"""{"refreshToken":"{{token1}}"}"""));
        Assert.Equal(HttpStatusCode.Unauthorized, stolenUse.StatusCode);

        var familyDead = await client.PostAsync("/api/auth/refresh", JsonText(
            $$"""{"refreshToken":"{{token2}}"}"""));
        Assert.Equal(HttpStatusCode.Unauthorized, familyDead.StatusCode);
    }

    [Fact]
    public async Task Logout_RevokesToken_SoSubsequentRefreshFails()
    {
        var client = NewClient();
        var register = await client.PostAsync("/api/auth/register", Json(
            new GPAHub.Application.DTOs.Student.RegisterStudentDto("Bye User", $"bye_{Guid.NewGuid():N}@test.com", "Passw0rd!")));
        var auth = await register.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
        var refreshToken = auth.GetProperty("refreshToken").GetString()!;

        var logout = await client.PostAsync("/api/auth/logout", JsonText(
            $$"""{"refreshToken":"{{refreshToken}}"}"""));
        Assert.Equal(HttpStatusCode.OK, logout.StatusCode);

        var afterLogout = await client.PostAsync("/api/auth/refresh", JsonText(
            $$"""{"refreshToken":"{{refreshToken}}"}"""));
        Assert.Equal(HttpStatusCode.Unauthorized, afterLogout.StatusCode);

        var logoutAgain = await client.PostAsync("/api/auth/logout", JsonText(
            $$"""{"refreshToken":"{{refreshToken}}"}"""));
        Assert.Equal(HttpStatusCode.OK, logoutAgain.StatusCode);
    }

    [Fact]
    public void Domain_RejectsExpiryBeforeCreation()
    {
        var now = DateTimeOffset.UtcNow;

        Assert.Throws<DomainException>(() =>
            new RefreshToken(Guid.NewGuid(), "hash", now, now.AddSeconds(-1)));
    }
}
