using GPAHub.Domain.Exceptions;

namespace GPAHub.Domain.ValueObjects;

public sealed record CreditHours
{
    public decimal Value { get; }

    public CreditHours(decimal value)
    {
        if (value <= 0m)
        {
            throw new DomainException("Credit hours must be greater than zero.");
        }

        Value = value;
    }
}
