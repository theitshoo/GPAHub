using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;

namespace GPAHub.Tests.IntegrationTests;

[Collection("Api")]
public class PaymentsEndpointTests : ApiTestBase
{
    private const string WebhookSecret = GpaHubApiFactory.StripeWebhookSecret;

    public PaymentsEndpointTests(GpaHubApiFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task Webhook_WithValidSignature_ActivatesPremium_Idempotently()
    {
        var (_, _, studentId, _) = await CreateUserAsync();

        var sessionId = $"cs_test_{Guid.NewGuid():N}";
        var eventBody = BuildCheckoutCompletedEvent(sessionId, studentId, 30);
        var first = await SendWebhookAsync(eventBody, timestamp: null);
        if (first.StatusCode != HttpStatusCode.OK) { var b = await first.Content.ReadAsStringAsync(); throw new System.Exception($"DBG500: {(int)first.StatusCode} | {b}"); }
        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        Assert.True((await first.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("handled").GetBoolean());

        await using var context = Factory.CreateDbContext();
        var subscriptions = context.Subscriptions.Where(s => s.StudentId == studentId).ToList();
        Assert.Single(subscriptions);
        Assert.Equal(Domain.Enums.SubscriptionType.Premium, subscriptions[0].Type);
        Assert.Equal(Domain.Enums.PaymentStatus.Completed, context.Payments.Single().Status);

        var replay = await SendWebhookAsync(eventBody, timestamp: null);
        Assert.Equal(HttpStatusCode.OK, replay.StatusCode);
        Assert.True((await replay.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("handled").GetBoolean());

        context.ChangeTracker.Clear();
        Assert.Single(context.Subscriptions.Where(s => s.StudentId == studentId).ToList());
    }

    [Fact]
    public async Task Webhook_WithInvalidSignature_IsUnauthorized()
    {
        var eventBody = BuildCheckoutCompletedEvent($"cs_test_{Guid.NewGuid():N}", Guid.NewGuid(), 30);

        var response = await SendWebhookAsync(eventBody, timestamp: null, signingSecret: "wrong-secret");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Webhook_StaleTimestamp_IsRejected_EvenWithValidSignature()
    {
        var staleTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds() - 3600;
        var eventBody = BuildCheckoutCompletedEvent($"cs_test_{Guid.NewGuid():N}", Guid.NewGuid(), 30);

        var response = await SendWebhookAsync(eventBody, staleTimestamp);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Webhook_NonPremiumEvent_IsIgnored()
    {
        var (_, _, studentId, _) = await CreateUserAsync();

        var sessionId = $"cs_test_{Guid.NewGuid():N}";
        var eventBody =
            "{\"id\":\"evt_test\",\"type\":\"invoice.paid\"," +
            "\"data\":{\"object\":{" +
            $"\"id\":\"{sessionId}\"," +
            $"\"metadata\":{{\"studentId\":\"{studentId}\"}}" +
            "}}}";

        var response = await SendWebhookAsync(eventBody, timestamp: null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.False((await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("handled").GetBoolean());

        await using var context = Factory.CreateDbContext();
        Assert.Empty(context.Subscriptions.Where(s => s.StudentId == studentId).ToList());
    }
    private static string BuildCheckoutCompletedEvent(string sessionId, Guid studentId, int? durationDays)
    {
        var durationJson = durationDays.HasValue ? durationDays.Value.ToString() : "null";

        return
            "{\"id\":\"evt_test\"," +
            "\"type\":\"checkout.session.completed\"," +
            "\"data\":{\"object\":{" +
            $"\"id\":\"{sessionId}\"," +
            "\"payment_status\":\"paid\"," +
            $"\"metadata\":{{\"studentId\":\"{studentId}\",\"durationDays\":{durationJson}}}" +
            "}}}";
    }

    private static StringContent JsonText(string body) => new(body, Encoding.UTF8, "application/json");

    private Task<HttpResponseMessage> SendWebhookAsync(string body, long? timestamp, string? signingSecret = null)
    {
        var secret = signingSecret ?? WebhookSecret;
        var unixTime = timestamp ?? DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        var signature = Convert.ToHexString(
            new HMACSHA256(Encoding.UTF8.GetBytes(secret))
                .ComputeHash(Encoding.UTF8.GetBytes($"{unixTime}.{body}")));

        var request = new HttpRequestMessage(HttpMethod.Post, "/api/payments/webhook/stripe")
        {
            Content = JsonText(body)
        };
        request.Headers.Add("Stripe-Signature", $"t={unixTime},v1={signature}");

        return NewClient().SendAsync(request);
    }
}



