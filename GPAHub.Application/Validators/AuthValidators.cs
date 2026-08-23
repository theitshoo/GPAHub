using FluentValidation;
using GPAHub.Application.DTOs.Student;

namespace GPAHub.Application.Validators;

public class RegisterStudentDtoValidator : AbstractValidator<RegisterStudentDto>
{
    public const int MinimumPasswordLength = 8;

    public RegisterStudentDtoValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithErrorCode("student_name_required")
            .MaximumLength(150).WithErrorCode("student_name_too_long");

        RuleFor(x => x.Email)
            .NotEmpty().WithErrorCode("email_required")
            .EmailAddress().WithErrorCode("email_invalid")
            .MaximumLength(256).WithErrorCode("email_too_long");

        RuleFor(x => x.Password)
            .MinimumLength(MinimumPasswordLength).WithErrorCode("password_too_short")
            .Must(p => p.Any(char.IsLetter)).WithErrorCode("password_missing_letter")
            .Must(p => p.Any(char.IsDigit)).WithErrorCode("password_missing_digit");
    }
}

public class LoginRequestDtoValidator : AbstractValidator<LoginRequestDto>
{
    public LoginRequestDtoValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithErrorCode("email_required");

        RuleFor(x => x.Password)
            .NotEmpty().WithErrorCode("password_required");
    }
}
