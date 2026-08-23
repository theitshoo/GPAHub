using FluentValidation;
using GPAHub.Application.DTOs.Semester;
using GPAHub.Application.DTOs.Student;
using GPAHub.Application.DTOs.Subscription;
using GPAHub.Application.DTOs.Target;

namespace GPAHub.Application.Validators;

public class UpcomingCourseInputDtoValidator : AbstractValidator<UpcomingCourseInputDto>
{
    public UpcomingCourseInputDtoValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithErrorCode("course_name_required")
            .MaximumLength(200).WithErrorCode("course_name_too_long");

        RuleFor(x => x.CreditHours)
            .GreaterThan(0m).WithErrorCode("credit_hours_positive")
            .LessThanOrEqualTo(60m).WithErrorCode("credit_hours_too_large");
    }
}

public class TargetPredictionRequestDtoValidator : AbstractValidator<TargetPredictionRequestDto>
{
    public TargetPredictionRequestDtoValidator()
    {
        RuleFor(x => x.CurrentGpa)
            .InclusiveBetween(0m, 1000m).WithErrorCode("current_gpa_out_of_range");

        RuleFor(x => x.CompletedCreditHours)
            .InclusiveBetween(0m, 1000m).WithErrorCode("completed_hours_out_of_range");

        RuleFor(x => x.TargetGpa)
            .InclusiveBetween(0m, 1000m).WithErrorCode("target_gpa_out_of_range");

        RuleFor(x => x.UpcomingCourses)
            .NotEmpty().WithErrorCode("upcoming_courses_required");

        RuleForEach(x => x.UpcomingCourses)
            .SetValidator(new UpcomingCourseInputDtoValidator());

        RuleForEach(x => x.CustomScaleDefinitions!)
            .SetValidator(new SaveGradeDefinitionDtoValidator())
            .When(x => x.CustomScaleDefinitions is { Count: > 0 });
    }
}

public class UpdateProfileDtoValidator : AbstractValidator<UpdateProfileDto>
{
    public UpdateProfileDtoValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithErrorCode("student_name_required")
            .MaximumLength(150).WithErrorCode("student_name_too_long");
    }
}

public class UpdateBaselineDtoValidator : AbstractValidator<UpdateBaselineDto>
{
    public UpdateBaselineDtoValidator()
    {
        RuleFor(x => x.CurrentGpa)
            .InclusiveBetween(0m, 1000m).WithErrorCode("baseline_gpa_out_of_range");

        RuleFor(x => x.CompletedCreditHours)
            .InclusiveBetween(0m, 1000m).WithErrorCode("baseline_hours_out_of_range");
    }
}

public class CreateSemesterDtoValidator : AbstractValidator<CreateSemesterDto>
{
    public CreateSemesterDtoValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithErrorCode("semester_name_required")
            .MaximumLength(100).WithErrorCode("semester_name_too_long");
    }
}

public class UpgradeToPremiumDtoValidator : AbstractValidator<UpgradeToPremiumDto>
{
    public UpgradeToPremiumDtoValidator()
    {
        RuleFor(x => x.Amount)
            .InclusiveBetween(0m, 1_000_000m).WithErrorCode("payment_amount_out_of_range");

        RuleFor(x => x.Currency)
            .Matches("^[A-Za-z]{3}$").WithErrorCode("currency_invalid");

        RuleFor(x => x.ExternalReference)
            .NotEmpty().WithErrorCode("external_reference_required")
            .MaximumLength(200).WithErrorCode("external_reference_too_long");

        RuleFor(x => x.DurationDays)
            .InclusiveBetween(1, 36_500).WithErrorCode("duration_days_out_of_range")
            .When(x => x.DurationDays.HasValue);
    }
}
