using FluentValidation;
using GPAHub.Application.DTOs.Gpa;

namespace GPAHub.Application.Validators;

public class GpaCourseInputDtoValidator : AbstractValidator<GpaCourseInputDto>
{
    public GpaCourseInputDtoValidator()
    {
        RuleFor(x => x.Name)
            .MaximumLength(200).WithErrorCode("course_name_too_long")
            .When(x => x.Name is not null);

        RuleFor(x => x.CreditHours)
            .InclusiveBetween(0m, 60m).WithErrorCode("credit_hours_out_of_range");

        RuleFor(x => x.NumericMark)
            .NotNull().WithErrorCode("numeric_mark_required")
            .When(x => x.InputType == Domain.Enums.GradeInputType.NumericMark);

        RuleFor(x => x.NumericMark)
            .InclusiveBetween(0, 100).WithErrorCode("mark_out_of_range")
            .When(x => x.InputType == Domain.Enums.GradeInputType.NumericMark && x.NumericMark.HasValue);

        RuleFor(x => x.LetterGrade)
            .NotEmpty().WithErrorCode("letter_grade_required")
            .MaximumLength(30).WithErrorCode("letter_grade_too_long")
            .When(x => x.InputType == Domain.Enums.GradeInputType.LetterGrade);
    }
}

public class CalculateGpaRequestDtoValidator : AbstractValidator<CalculateGpaRequestDto>
{
    public CalculateGpaRequestDtoValidator()
    {
        RuleFor(x => x.Courses)
            .NotEmpty().WithErrorCode("courses_required");

        RuleForEach(x => x.Courses)
            .SetValidator(new GpaCourseInputDtoValidator());

        RuleFor(x => x.BaselineCreditHours)
            .InclusiveBetween(0m, 1000m).WithErrorCode("baseline_hours_out_of_range")
            .When(x => x.BaselineCreditHours.HasValue);

        RuleFor(x => x.BaselineGpa)
            .InclusiveBetween(0m, 1000m).WithErrorCode("baseline_gpa_out_of_range")
            .When(x => x.BaselineGpa.HasValue);

        RuleFor(x => x)
            .Must(x => x.BaselineGpa.HasValue == x.BaselineCreditHours.HasValue)
            .WithErrorCode("baseline_incomplete")
            .WithMessage("Baseline GPA and completed credit hours must be provided together.");

        RuleForEach(x => x.CustomScaleDefinitions!)
            .SetValidator(new SaveGradeDefinitionDtoValidator())
            .When(x => x.CustomScaleDefinitions is { Count: > 0 });
    }
}
