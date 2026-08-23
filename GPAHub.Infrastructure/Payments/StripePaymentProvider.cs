using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using GPAHub.Application.Interfaces.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GPAHub.Infrastructure.Payments;

public sealed class StripeOptions
{
    public const string SectionName = "Stripe";

    public string SecretKey { get; init; } = string.Empty;

    public string WebhookSecret { get; init; } = string.Empty;

    public string SuccessUrl { get; init; } = "https://checkout.gpahub.local/success";

    public string CancelUrl { get; init; } = "https://checkout.gpahub.local/cancelled";

    public int SignatureToleranceMinutes { get; init; } = 5;
}

public class StripePaymentProvider : IPaymentGateway
{
    private const string CheckoutEndpoint = "https://api.stripe.com/v1/checkout/sessions";

    private readonly StripeOptions _options;
    private readonly HttpClient _httpClient;
    private readonly ILogger<StripePaymentProvider> _logger;

    public StripePaymentProvider(IOptions<StripeOptions> options, HttpClient httpClient, ILogger<StripePaymentProvider> logger)
    {
        _options = options.Value;
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<CheckoutSessionResult> CreatePremiumCheckoutSessionAsync(
        CheckoutSessionRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_options.SecretKey))
        {
            throw new InvalidOperationException("Stripe payments are not configured (Stripe:SecretKey missing).");
        }

        var amountMinorUnits = (long)(request.Amount * 100m);
        var currency = request.Currency.ToLowerInvariant();

        var form = new Dictionary<string, string>
        {
            ["mode"] = "payment",
            ["success_url"] = _options.SuccessUrl,
            ["cancel_url"] = _options.CancelUrl,
            ["line_items[0][quantity]"] = "1",
            ["line_items[0][price_data][currency]"] = currency,
            ["line_items[0][price_data][unit_amount]"] = amountMinorUnits.ToString(),
            ["line_items[0][price_data][product_data][name]"] = "GPAHub Premium",
            ["metadata[studentId]"] = request.StudentId.ToString(),
            ["metadata[durationDays]"] = request.DurationDays?.ToString() ?? string.Empty
        };

        using var content = new FormUrlEncodedContent(form);

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, CheckoutEndpoint) { Content = content };
        httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.SecretKey);

        using var httpResponse = await _httpClient.SendAsync(httpRequest, cancellationToken);
        var body = await httpResponse.Content.ReadAsStringAsync(cancellationToken);

        if (!httpResponse.IsSuccessStatusCode)
        {
            _logger.LogError("Stripe checkout session creation failed ({StatusCode}): {Body}",
                (int)httpResponse.StatusCode, body);
            throw new InvalidOperationException("The payment provider rejected the checkout session request.");
        }

        using var document = JsonDocument.Parse(body);
        var sessionId = document.RootElement.GetProperty("id").GetString()
            ?? throw new InvalidOperationException("Stripe response did not contain a session id.");
        var checkoutUrl = document.RootElement.GetProperty("url").GetString()
            ?? throw new InvalidOperationException("Stripe response did not contain a checkout url.");

        return new CheckoutSessionResult(sessionId, checkoutUrl);
    }

    public bool VerifyWebhookSignature(string rawBody, string signatureHeader)
    {
        if (string.IsNullOrWhiteSpace(_options.WebhookSecret) ||
            string.IsNullOrWhiteSpace(rawBody) ||
            string.IsNullOrWhiteSpace(signatureHeader))
        {
            return false;
        }

        long timestamp = 0;
        var providedSignatures = new List<string>();

        foreach (var segment in signatureHeader.Split(','))
        {
            var parts = segment.Split('=', 2);
            if (parts.Length != 2)
            {
                continue;
            }

            if (parts[0].Trim() == "t" && long.TryParse(parts[1].Trim(), out var parsed))
            {
                timestamp = parsed;
            }
            else if (parts[0].Trim() == "v1")
            {
                providedSignatures.Add(parts[1].Trim());
            }
        }

        if (timestamp == 0 || providedSignatures.Count == 0)
        {
            return false;
        }

        var age = DateTimeOffset.UtcNow.ToUnixTimeSeconds() - timestamp;
        if (Math.Abs(age) > _options.SignatureToleranceMinutes * 60)
        {
            return false;
        }

        var signedPayload = $"{timestamp}.{rawBody}";
        var expected = Convert.ToHexString(
            new HMACSHA256(Encoding.UTF8.GetBytes(_options.WebhookSecret))
                .ComputeHash(Encoding.UTF8.GetBytes(signedPayload)));

        return providedSignatures.Any(provided =>
            CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(provided),
                Encoding.UTF8.GetBytes(expected)));
    }
}

