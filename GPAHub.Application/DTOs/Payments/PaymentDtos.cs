namespace GPAHub.Application.DTOs.Payments;

public sealed record BeginUpgradeResponseDto(string CheckoutUrl, string ExternalReference);
