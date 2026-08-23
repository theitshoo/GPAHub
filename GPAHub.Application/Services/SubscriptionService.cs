using AutoMapper;
using FluentValidation;
using GPAHub.Application.Common;
using GPAHub.Application.DTOs.Subscription;
using GPAHub.Application.Interfaces.Repositories;
using GPAHub.Application.Interfaces.Services;
using GPAHub.Application.Validators;
using GPAHub.Domain.Entities;
using GPAHub.Domain.Enums;
using GPAHub.Domain.Exceptions;

namespace GPAHub.Application.Services;

public class SubscriptionService : ISubscriptionService
{
    private readonly ISubscriptionRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly UpgradeToPremiumDtoValidator _validator;

    public SubscriptionService(ISubscriptionRepository repository, IUnitOfWork unitOfWork, UpgradeToPremiumDtoValidator validator)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _validator = validator;
    }

    public async Task<bool> IsPremiumAsync(Guid studentId, CancellationToken cancellationToken = default)
    {
        var subscription = await _repository.GetLatestForStudentAsync(studentId, cancellationToken);

        return subscription is { Type: SubscriptionType.Premium } &&
               subscription.IsActiveAsOf(DateTimeOffset.UtcNow);
    }

    public async Task<SubscriptionDto> GetCurrentAsync(Guid studentId, CancellationToken cancellationToken = default)
    {
        var subscription = await _repository.GetLatestForStudentAsync(studentId, cancellationToken);
        if (subscription is null)
        {
            return new SubscriptionDto(
                Id: null,
                Type: SubscriptionType.Free,
                Status: SubscriptionStatus.Active,
                StartDate: DateTimeOffset.UtcNow,
                EndDate: null,
                IsActive: true);
        }

        var dto = new SubscriptionDto(
            subscription.Id,
            subscription.Type,
            subscription.Status,
            subscription.StartDate,
            subscription.EndDate,
            subscription.IsActiveAsOf(DateTimeOffset.UtcNow));

        return dto;
    }

    public async Task<Result<SubscriptionDto>> UpgradeToPremiumAsync(Guid studentId, UpgradeToPremiumDto dto, CancellationToken cancellationToken = default)
    {
        var validation = await _validator.ValidateAsync(dto, cancellationToken);
        if (!validation.IsValid)
        {
            return Result<SubscriptionDto>.Fail(ValidationErrors.From(validation));
        }

        try
        {
            var current = await _repository.GetLatestForStudentAsync(studentId, cancellationToken);
            if (current is { Type: SubscriptionType.Premium } && current.IsActiveAsOf(DateTimeOffset.UtcNow))
            {
                return Result<SubscriptionDto>.Fail(Error.Conflict("already_premium", "An active Premium subscription already exists."));
            }

            var now = DateTimeOffset.UtcNow;
            var premium = new Subscription(
                studentId,
                SubscriptionType.Premium,
                now,
                dto.DurationDays.HasValue ? now.AddDays(dto.DurationDays.Value) : null);

            var payment = premium.AddPayment(dto.Amount, dto.Currency, now, dto.ExternalReference);
            payment.MarkCompleted();

            if (current is not null)
            {
                current.Expire();
                _repository.Update(current);
            }

            await _repository.AddAsync(premium, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Result<SubscriptionDto>.Ok(new SubscriptionDto(
                premium.Id,
                premium.Type,
                premium.Status,
                premium.StartDate,
                premium.EndDate,
                premium.IsActiveAsOf(now)));
        }
        catch (DomainException exception)
        {
            return Result<SubscriptionDto>.Fail(DomainResult.ToError(exception));
        }
    }

    public async Task<Result> CancelAsync(Guid studentId, CancellationToken cancellationToken = default)
    {
        var subscription = await _repository.GetLatestForStudentAsync(studentId, cancellationToken);
        if (subscription is null || !subscription.IsActiveAsOf(DateTimeOffset.UtcNow))
        {
            return Result.Fail(Error.NotFound("no_active_subscription", "No active subscription was found."));
        }

        subscription.Expire();
        _repository.Update(subscription);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Ok();
    }
}
