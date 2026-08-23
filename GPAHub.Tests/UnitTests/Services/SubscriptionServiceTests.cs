using GPAHub.Application.Common;
using GPAHub.Application.DTOs.Subscription;
using GPAHub.Application.Interfaces.Repositories;
using GPAHub.Application.Interfaces.Services;
using GPAHub.Application.Services;
using GPAHub.Application.Validators;
using GPAHub.Domain.Entities;
using GPAHub.Domain.Enums;
using Moq;

namespace GPAHub.Tests.UnitTests.Services;

public class SubscriptionServiceTests
{
    private readonly Mock<ISubscriptionRepository> _repo = new();
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<IPremiumActivationService> _activation = new();
    private readonly SubscriptionService _service;

    public SubscriptionServiceTests()
    {
        _service = new SubscriptionService(_repo.Object, _uow.Object, _activation.Object, new UpgradeToPremiumDtoValidator());

        _activation.Setup(a => a.CreateActivePremiumAsync(
                It.IsAny<Guid>(), It.IsAny<DateTimeOffset>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid studentId, DateTimeOffset now, int? durationDays, CancellationToken _) =>
            {
                var premium = new Subscription(studentId, SubscriptionType.Premium, now,
                    durationDays.HasValue ? now.AddDays(durationDays.Value) : null);
                premium.AddPayment(9.99m, "USD", now, $"act-{Guid.NewGuid():N}").MarkCompleted();
                return premium;
            });
    }

    [Fact]
    public async Task IsPremium_WithNoRecords_ReturnsFalse()
    {
        var result = await _service.IsPremiumAsync(Guid.NewGuid());

        Assert.False(result);
    }

    [Fact]
    public async Task IsPremium_ActivePremium_ReturnsTrue()
    {
        var subscription = new Subscription(Guid.NewGuid(), SubscriptionType.Premium, DateTimeOffset.UtcNow.AddDays(-1), null);
        SetupLatest(subscription);

        var result = await _service.IsPremiumAsync(subscription.StudentId);

        Assert.True(result);
    }

    [Fact]
    public async Task IsPremium_ExpiredPremium_ReturnsFalse()
    {
        var subscription = new Subscription(
            Guid.NewGuid(), SubscriptionType.Premium,
            DateTimeOffset.UtcNow.AddDays(-10), DateTimeOffset.UtcNow.AddDays(-1));
        SetupLatest(subscription);

        var result = await _service.IsPremiumAsync(subscription.StudentId);

        Assert.False(result);
    }

    [Fact]
    public async Task IsPremium_FreeNeverPremium()
    {
        var subscription = new Subscription(Guid.NewGuid(), SubscriptionType.Free, DateTimeOffset.UtcNow, null);
        SetupLatest(subscription);

        Assert.False(await _service.IsPremiumAsync(subscription.StudentId));
    }

    [Fact]
    public async Task GetCurrent_WithoutRecord_ReturnsImplicitFree()
    {
        var dto = await _service.GetCurrentAsync(Guid.NewGuid());

        Assert.Null(dto.Id);
        Assert.Equal(SubscriptionType.Free, dto.Type);
        Assert.True(dto.IsActive);
    }

    [Fact]
    public async Task Upgrade_CreatesActivePremium_WithCompletedPayment_AndExpiresPrevious()
    {
        var oldFree = new Subscription(StudentId, SubscriptionType.Free, DateTimeOffset.UtcNow.AddMonths(-2), null);
        SetupLatest(oldFree);

        var result = await _service.UpgradeToPremiumAsync(StudentId,
            new UpgradeToPremiumDto(9.99m, "usd", "txn-77", DurationDays: 30));

        Assert.True(result.IsSuccess);
        Assert.Equal(SubscriptionType.Premium, result.Value.Type);
        Assert.True(result.Value.IsActive);

        _activation.Verify(a => a.CreateActivePremiumAsync(
            StudentId, It.IsAny<DateTimeOffset>(), 30, It.IsAny<CancellationToken>()), Times.Once);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Upgrade_Lifetime_WhenDurationNull()
    {
        SetupLatest(null);

        var result = await _service.UpgradeToPremiumAsync(StudentId,
            new UpgradeToPremiumDto(49m, "USD", "txn-life", DurationDays: null));

        Assert.True(result.IsSuccess);
        Assert.Null(result.Value.EndDate);
    }

    [Fact]
    public async Task Upgrade_AlreadyPremium_ReturnsConflict()
    {
        var premium = new Subscription(StudentId, SubscriptionType.Premium, DateTimeOffset.UtcNow.AddDays(-1), null);
        SetupLatest(premium);

        var result = await _service.UpgradeToPremiumAsync(StudentId,
            new UpgradeToPremiumDto(10m, "USD", "txn-x"));

        Assert.Equal(ErrorType.Conflict, result.Error!.Type);
    }

    [Fact]
    public async Task Upgrade_InvalidCurrency_FailsValidation_WithoutSaving()
    {
        SetupLatest(null);

        var result = await _service.UpgradeToPremiumAsync(StudentId,
            new UpgradeToPremiumDto(10m, "dollars", "txn-y"));

        Assert.Equal(ErrorType.Validation, result.Error!.Type);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Cancel_ExpiresActivePremium()
    {
        var premium = new Subscription(StudentId, SubscriptionType.Premium, DateTimeOffset.UtcNow.AddDays(-1), null);
        SetupLatest(premium);

        var result = await _service.CancelAsync(StudentId);

        Assert.True(result.IsSuccess);
        Assert.False(premium.IsActiveAsOf(DateTimeOffset.UtcNow));
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Cancel_WithoutActivePremium_Fails()
    {
        SetupLatest(null);

        var result = await _service.CancelAsync(StudentId);

        Assert.True(result.IsFailure);
    }

    private static readonly Guid StudentId = Guid.NewGuid();

    private void SetupLatest(Subscription? subscription) =>
        _repo.Setup(r => r.GetLatestForStudentAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(subscription);
}

