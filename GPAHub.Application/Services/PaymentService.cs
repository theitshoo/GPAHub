using AutoMapper;
using FluentValidation;
using GPAHub.Application.Common;
using GPAHub.Application.DTOs.Payments;
using GPAHub.Application.DTOs.Subscription;
using GPAHub.Application.Interfaces.Repositories;
using GPAHub.Application.Interfaces.Services;
using GPAHub.Application.Validators;
using GPAHub.Domain.Entities;
using GPAHub.Domain.Enums;
using GPAHub.Domain.Exceptions;
using GPAHub.Domain.ValueObjects;

namespace GPAHub.Application.Services;

public class PaymentService : IPaymentService
{
    private readonly IPaymentGateway _gateway;
    private readonly IPaymentRepository _paymentRepository;
    private readonly ISubscriptionRepository _subscriptionRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPremiumActivationService _premiumActivation;
    private readonly UpgradeToPremiumDtoValidator _validator = new();

    public PaymentService(
        IPaymentGateway gateway,
        IPaymentRepository paymentRepository,
        ISubscriptionRepository subscriptionRepository,
        IUnitOfWork unitOfWork,
        IPremiumActivationService premiumActivation)
    {
        _gateway = gateway;
        _paymentRepository = paymentRepository;
        _subscriptionRepository = subscriptionRepository;
        _unitOfWork = unitOfWork;
        _premiumActivation = premiumActivation;
    }

    public async Task<Result<BeginUpgradeResponseDto>> BeginPremiumUpgradeAsync(Guid studentId, UpgradeToPremiumDto dto, CancellationToken cancellationToken = default)
    {
        var validation = await _validator.ValidateAsync(dto, cancellationToken);
        if (!validation.IsValid)
        {
            return Result<BeginUpgradeResponseDto>.Fail(ValidationErrors.From(validation));
        }

        var current = await _subscriptionRepository.GetLatestForStudentAsync(studentId, cancellationToken);
        if (current is { Type: SubscriptionType.Premium } && current.IsActiveAsOf(DateTimeOffset.UtcNow))
        {
            return Result<BeginUpgradeResponseDto>.Fail(
                Error.Conflict("already_premium", "An active Premium subscription already exists."));
        }

        var request = new CheckoutSessionRequest(
            studentId,
            dto.Amount,
            dto.Currency,
            dto.DurationDays,
            SuccessUrl: "https://checkout.gpahub.local/success",
            CancelUrl: "https://checkout.gpahub.local/cancelled");

        CheckoutSessionResult session;

        try
        {
            session = await _gateway.CreatePremiumCheckoutSessionAsync(request, cancellationToken);
        }
        catch (InvalidOperationException exception)
        {
            return Result<BeginUpgradeResponseDto>.Fail(Error.Failure("payments_unavailable", exception.Message));
        }

        try
        {
            var payment = Payment.CreatePending(null, dto.Amount, dto.Currency, DateTimeOffset.UtcNow, session.SessionId);

            await _paymentRepository.AddAsync(payment, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Result<BeginUpgradeResponseDto>.Ok(new BeginUpgradeResponseDto(session.CheckoutUrl, session.SessionId));
        }
        catch (DomainException exception)
        {
            return Result<BeginUpgradeResponseDto>.Fail(DomainResult.ToError(exception));
        }
    }

    public async Task<Result> ApplyPremiumPaymentSucceededAsync(string sessionId, Guid studentId, int? durationDays, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(sessionId))
        {
            return Result.Fail(Error.Validation("session_reference_required", "Payment session reference is required."));
        }

        var existingPayment = await _paymentRepository.GetByExternalReferenceAsync(sessionId, cancellationToken);

        if (existingPayment is { Status: PaymentStatus.Completed })
        {
            return Result.Ok();
        }

        var now = DateTimeOffset.UtcNow;

        try
        {
            var subscription = await _premiumActivation.CreateActivePremiumAsync(studentId, now, durationDays, cancellationToken);

            if (existingPayment is { Status: PaymentStatus.Pending } pending)
            {
                pending.AttachToSubscription(subscription.Id);
                pending.MarkCompleted();
            }
            else
            {
                subscription.AddPayment(0m, "USD", now, sessionId).MarkCompleted();
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Result.Ok();
        }
        catch (DomainException exception)
        {
            return Result.Fail(DomainResult.ToError(exception));
        }
    }

    public bool IsWebhookSignatureValid(string rawBody, string signatureHeader) =>
        _gateway.VerifyWebhookSignature(rawBody, signatureHeader);
}
