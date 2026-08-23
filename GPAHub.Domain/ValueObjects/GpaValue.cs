using GPAHub.Domain.Constants;
using GPAHub.Domain.Exceptions;

namespace GPAHub.Domain.ValueObjects;

public sealed record GpaValue
{
    public const int DisplayDecimalPlaces = GpaConstants.DisplayDecimalPlaces;

    public decimal Value { get; }

    public GpaValue(decimal value)
    {
        if (value < 0m)
        {
            throw new DomainException("GPA value cannot be negative.");
        }

        Value = value;
    }

    public decimal Rounded => Math.Round(Value, DisplayDecimalPlaces, MidpointRounding.AwayFromZero);
}
