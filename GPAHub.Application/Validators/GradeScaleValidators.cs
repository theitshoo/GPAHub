using FluentValidation;
using GPAHub.Application.DTOs.GradeScale;

namespace GPAHub.Application.Validators;

public class SaveGradeDefinitionDtoValidator : AbstractValidator<SaveGradeDefinitionDto>
{
    public SaveGradeDefinitionDtoValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithErrorCode("grade_name_required")
            .MaximumLength(30).WithErrorCode("grade_name_too_long");

        RuleFor(x => x.MinMark)
            .InclusiveBetween(0, 100).WithErrorCode("mark_out_of_range");

        RuleFor(x => x.MaxMark)
            .InclusiveBetween(0, 100).WithErrorCode("mark_out_of_range");

        RuleFor(x => x)
            .Must(x => x.MinMark <= x.MaxMark)
            .WithErrorCode("min_greater_than_max")
            .WithMessage("Minimum mark must be less than or equal to maximum mark.");

        RuleFor(x => x.Points)
            .InclusiveBetween(0m, 1000m).WithErrorCode("points_out_of_range");
    }
}

public class CreateGradeScaleDtoValidator : AbstractValidator<CreateGradeScaleDto>
{
    public CreateGradeScaleDtoValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithErrorCode("scale_name_required")
            .MaximumLength(100).WithErrorCode("scale_name_too_long");

        RuleFor(x => x.Description)
            .MaximumLength(500).WithErrorCode("scale_description_too_long");
    }
}

public class UpdateGradeScaleDtoValidator : AbstractValidator<UpdateGradeScaleDto>
{
    public UpdateGradeScaleDtoValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithErrorCode("scale_name_required")
            .MaximumLength(100).WithErrorCode("scale_name_too_long");

        RuleFor(x => x.Description)
            .MaximumLength(500).WithErrorCode("scale_description_too_long");
    }
}
