using GPAHub.Application.Common;
using GPAHub.Application.DTOs.GradeScale;
using GPAHub.Application.Interfaces.Repositories;
using GPAHub.Domain.Entities;
using GPAHub.Domain.Exceptions;

namespace GPAHub.Application.Services;

public static class ScaleResolver
{
    public static async Task<Result<GradeScale>> ResolveAsync(
        IGradeScaleRepository repository,
        Guid? studentId,
        IReadOnlyList<SaveGradeDefinitionDto>? customDefinitions,
        Guid? scaleId,
        CancellationToken cancellationToken = default)
    {
        if (customDefinitions is { Count: > 0 })
        {
            try
            {
                var customScale = new GradeScale("Custom Scale", studentId: null);
                foreach (var definition in customDefinitions)
                {
                    customScale.AddDefinition(definition.Name, definition.MinMark, definition.MaxMark, definition.Points);
                }

                customScale.EnsureValid();

                return Result<GradeScale>.Ok(customScale);
            }
            catch (DomainException exception)
            {
                return Result<GradeScale>.Fail(DomainResult.ToError(exception));
            }
        }

        if (scaleId.HasValue)
        {
            if (!studentId.HasValue)
            {
                return Result<GradeScale>.Fail(
                    Error.Validation("scale_id_requires_authentication", "Selecting a specific scale requires an authenticated student."));
            }

            var owned = await repository.GetByIdForStudentAsync(scaleId.Value, studentId.Value, cancellationToken);
            if (owned is null)
            {
                return Result<GradeScale>.Fail(Error.NotFound("scale_not_found", "Grade scale was not found."));
            }

            return Result<GradeScale>.Ok(owned);
        }

        if (studentId.HasValue)
        {
            var active = await repository.GetActiveForStudentAsync(studentId.Value, cancellationToken);
            if (active is not null)
            {
                return Result<GradeScale>.Ok(active);
            }
        }

        var systemDefault = await repository.GetSystemDefaultAsync(cancellationToken);
        if (systemDefault is not null)
        {
            return Result<GradeScale>.Ok(systemDefault);
        }

        return Result<GradeScale>.Fail(Error.NotFound("default_scale_missing", "No grade scale is available for the calculation."));
    }
}
