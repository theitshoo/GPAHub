using GPAHub.Application.Interfaces.Repositories;
using GPAHub.Application.Services;
using GPAHub.Domain.Entities;
using GPAHub.Domain.Enums;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace GPAHub.Tests.UnitTests.Services;

public class PremiumActivationServiceTests
{
    private readonly Mock<ISubscriptionRepository> _repo = new();
    private readonly PremiumActivationService _service;

    public PremiumActivationServiceTests()
    {
        _service = new PremiumActivationService(_repo.Object, NullLogger<PremiumActivationService>.Instance);
    }

    [Fact]
    public async Task Activate_WithoutExistingSubscription_CreatesActivePremium()
    {
        var premium = await _service.CreateActivePremiumAsync(Guid.NewGuid(), Now, 30, CancellationToken.None);

        Assert.Equal(SubscriptionType.Premium, premium.Type);
        Assert.Equal(SubscriptionStatus.Active, premium.Status);
        Assert.NotNull(premium.EndDate);
        Assert.Empty(premium.Payments);
        _repo.Verify(r => r.AddAsync(premium, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Activate_ExpirePreviousSubscription_AndAddsNewOne()
    {
        var previous = new Subscription(Guid.NewGuid(), SubscriptionType.Premium, Now.AddDays(-30), null);
        SetupLatest(previous);

        await _service.CreateActivePremiumAsync(previous.StudentId, Now, null, CancellationToken.None);

        Assert.Equal(SubscriptionStatus.Expired, previous.Status);
        _repo.Verify(r => r.Update(previous), Times.Once);
    }

    [Fact]
    public async Task Activate_WithNullDuration_GrantsLifetimeEndDate()
    {
        var premium = await _service.CreateActivePremiumAsync(Guid.NewGuid(), Now, null, CancellationToken.None);

        Assert.Null(premium.EndDate);
        Assert.True(premium.IsActiveAsOf(Now.AddYears(5)));
    }

    private static DateTimeOffset Now => DateTimeOffset.UtcNow;

    private void SetupLatest(Subscription subscription) =>
        _repo.Setup(r => r.GetLatestForStudentAsync(subscription.StudentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(subscription);
}
