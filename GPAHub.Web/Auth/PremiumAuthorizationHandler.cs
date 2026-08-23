using System.Security.Claims;
using GPAHub.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;

namespace GPAHub.Web.Auth;

public sealed class PremiumRequirement : IAuthorizationRequirement
{
}

public class PremiumAuthorizationHandler : AuthorizationHandler<PremiumRequirement>
{
    private readonly ISubscriptionService _subscriptions;

    public PremiumAuthorizationHandler(ISubscriptionService subscriptions)
    {
        _subscriptions = subscriptions;
    }

    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, PremiumRequirement requirement)
    {
        if (!Guid.TryParse(context.User.FindFirstValue(ClaimTypes.NameIdentifier), out var studentId))
        {
            return;
        }

        if (await _subscriptions.IsPremiumAsync(studentId))
        {
            context.Succeed(requirement);
        }
    }
}
