namespace GPAHub.Domain.Exceptions;

public sealed class InvalidGradeScaleException : DomainException
{
    public IReadOnlyList<string> Errors { get; }

    public InvalidGradeScaleException(IReadOnlyList<string> errors)
        : base("The grade scale is invalid: " + string.Join("; ", errors))
    {
        Errors = errors;
    }
}
