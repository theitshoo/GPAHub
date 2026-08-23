using FluentValidation.Results;
using GPAHub.Application.Common;

namespace GPAHub.Application.Services;

internal static class DomainResult
{
    public const string RuleViolationCode = "domain_rule_violation";

    public static Error ToError(Domain.Exceptions.DomainException exception) =>
        Error.Conflict(RuleViolationCode, exception.Message);
}

internal static class ValidationErrors
{
    public static Error From(ValidationResult validationResult)
    {
        if (validationResult.IsValid || validationResult.Errors.Count == 0)
        {
            return Error.Validation("validation_failed", "The request is invalid.");
        }

        var code = validationResult.Errors[0].ErrorCode;
        var message = string.Join("; ", validationResult.Errors
            .Select(e => string.IsNullOrWhiteSpace(e.ErrorMessage) ? e.ErrorCode : e.ErrorMessage)
            .Distinct());

        return Error.Validation(code, message);
    }
}
