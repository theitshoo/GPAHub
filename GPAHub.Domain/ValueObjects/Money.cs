using GPAHub.Domain.Exceptions;

namespace GPAHub.Domain.ValueObjects;

public sealed record Money
{
    public const string DefaultCurrency = "USD";

    private const int IsoCurrencyLength = 3;

    public decimal Amount { get; }

    public string Currency { get; }

    public Money(decimal amount, string currency = DefaultCurrency)
    {
        if (amount < 0m)
        {
            throw new DomainException("Money amount cannot be negative.");
        }

        if (string.IsNullOrWhiteSpace(currency))
        {
            throw new DomainException("Currency is required.");
        }

        var normalized = currency.Trim().ToUpperInvariant();

        if (normalized.Length != IsoCurrencyLength || !normalized.All(c => c is >= 'A' and <= 'Z'))
        {
            throw new DomainException("Currency must be a 3-letter code.");
        }

        Amount = amount;
        Currency = normalized;
    }
}
