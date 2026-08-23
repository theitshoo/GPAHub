using System.Text.Json;
using GPAHub.Application.Interfaces.Services;
using GPAHub.Application.DTOs.Subscription;
using GPAHub.Web.Controllers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GPAHub.Web.Controllers;

[Authorize]
[Route("api/payments")]
public class PaymentsController : ApiControllerBase
{
    private readonly IPaymentService _paymentService;
    private readonly IConfiguration _configuration;

    public PaymentsController(IPaymentService paymentService, IConfiguration configuration)
    {
        _paymentService = paymentService;
        _configuration = configuration;
    }

    [HttpPost("checkout")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> BeginPremiumUpgrade(UpgradeToPremiumDto dto, CancellationToken cancellationToken)
    {
        var result = await _paymentService.BeginPremiumUpgradeAsync(RequireStudentId(), dto, cancellationToken);

        return result.IsSuccess
            ? Ok(result.Value)
            : FromError(result.Error!);
    }

    [AllowAnonymous]
    [HttpPost("webhook/stripe")]
    [Consumes("application/json")]
    public async Task<IActionResult> StripeWebhook(CancellationToken cancellationToken)
    {
        Request.EnableBuffering();

        string rawBody;
        using (var reader = new StreamReader(Request.Body, leaveOpen: true))
        {
            rawBody = await reader.ReadToEndAsync(cancellationToken);
        }
        Request.Body.Position = 0;

        var signatureHeader = Request.Headers["Stripe-Signature"].FirstOrDefault();

        if (!_paymentService.IsWebhookSignatureValid(rawBody, signatureHeader))
        {
            return Unauthorized(new ProblemDetails
            {
                Status = StatusCodes.Status401Unauthorized,
                Title = "Invalid webhook signature."
            });
        }

        string? sessionId;
        Guid studentId;
        int? durationDays;

        try
        {
            using var document = JsonDocument.Parse(rawBody);
            var root = document.RootElement;

            sessionId = root.GetProperty("data").GetProperty("object").TryGetProperty("id", out var idElement)
                ? idElement.GetString()
                : null;

            var metadata = root.GetProperty("data").GetProperty("object").TryGetProperty("metadata", out var metaElement)
                ? metaElement
                : (JsonElement?)null;

            studentId = Guid.TryParse(GetStringOrNull(metadata, "studentId"), out var parsedStudentId)
                ? parsedStudentId
                : Guid.Empty;
            durationDays = int.TryParse(GetStringOrNull(metadata, "durationDays"), out var parsedDuration)
                ? parsedDuration
                : null;
        }
        catch (JsonException)
        {
            return BadRequest(new ProblemDetails { Status = 400, Title = "Malformed webhook payload." });
        }
        catch (KeyNotFoundException)
        {
            return Ok(new { handled = false });
        }

        if (sessionId is null || studentId == Guid.Empty)
        {
            return Ok(new { handled = false });
        }

        if (!IsPremiumCheckoutEvent(rawBody))
        {
            return Ok(new { handled = false });
        }

        var result = await _paymentService.ApplyPremiumPaymentSucceededAsync(sessionId, studentId, durationDays, cancellationToken);

        return result.IsSuccess ? Ok(new { handled = true }) : FromError(result.Error!);
    }

    private static bool IsPremiumCheckoutEvent(string rawBody)
    {
        try
        {
            using var document = JsonDocument.Parse(rawBody);
            var root = document.RootElement;
            if (!root.TryGetProperty("type", out var typeElement) ||
                typeElement.GetString() != "checkout.session.completed")
            {
                return false;
            }

            var session = root.GetProperty("data").GetProperty("object");
            return !session.TryGetProperty("payment_status", out var status) ||
                   GetStringOrNull(session, "payment_status") == "paid";
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private static string? GetStringOrNull(JsonElement? element, string propertyName)
    {
        if (element is null || !element.Value.TryGetProperty(propertyName, out var property))
        {
            return null;
        }

        return property.ValueKind switch
        {
            JsonValueKind.String => property.GetString(),
            JsonValueKind.Number => property.GetRawText(),
            _ => null
        };
    }
}
