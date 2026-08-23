using FluentValidation;
using GPAHub.Application.DTOs.Course;
using GPAHub.Domain.Enums;

namespace GPAHub.Application.Validators;

public class CourseInputDtoValidator : AbstractValidator<CourseInputDto>
{
    public CourseInputDtoValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithErrorCode("course_name_required")
            .MaximumLength(200).WithErrorCode("course_name_too_long");

        RuleFor(x => x.Code)
            .MaximumLength(50).WithErrorCode("course_code_too_long");

        RuleFor(x => x.CreditHours)
            .GreaterThan(0m).WithErrorCode("credit_hours_positive")
            .LessThanOrEqualTo(60m).WithErrorCode("credit_hours_too_large");

        RuleFor(x => x.NumericMark)
            .NotNull().WithErrorCode("numeric_mark_required")
            .When(x => x.InputType == GradeInputType.NumericMark);

        RuleFor(x => x.NumericMark)
            .InclusiveBetween(0, 100).WithErrorCode("mark_out_of_range")
            .When(x => x.InputType == GradeInputType.NumericMark && x.NumericMark.HasValue);

        RuleFor(x => x.LetterGrade)
            .NotEmpty().WithErrorCode("letter_grade_required")
            .MaximumLength(30).WithErrorCode("letter_grade_too_long")
            .When(x => x.InputType == GradeInputType.LetterGrade);

        RuleFor(x => x.LetterGrade)
            .Must(letter => string.IsNullOrWhiteSpace(letter))
            .WithErrorCode("conflicting_course_input")
            .When(x => x.InputType == GradeInputType.NumericMark);
    }
}
