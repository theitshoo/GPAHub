using GPAHub.Application.DTOs.Subscription;
using GPAHub.Application.Interfaces.Services;
using GPAHub.Web.Controllers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GPAHub.Web.Controllers;

[Authorize]
[Route("api/subscription")]
public class SubscriptionController : ApiControllerBase
{
    private readonly ISubscriptionService _subscriptionService;

    public SubscriptionController(ISubscriptionService subscriptionService)
    {
        _subscriptionService = subscriptionService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(SubscriptionDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<SubscriptionDto>> GetCurrent(CancellationToken cancellationToken) =>
        Ok(await _subscriptionService.GetCurrentAsync(RequireStudentId(), cancellationToken));

    [HttpPost("upgrade")]
    public async Task<ActionResult<SubscriptionDto>> Upgrade(UpgradeToPremiumDto dto, CancellationToken cancellationToken) =>
        FromResult(await _subscriptionService.UpgradeToPremiumAsync(RequireStudentId(), dto, cancellationToken));

    [HttpPost("cancel")]
    public async Task<IActionResult> Cancel(CancellationToken cancellationToken) =>
        FromResult(await _subscriptionService.CancelAsync(RequireStudentId(), cancellationToken));
}
