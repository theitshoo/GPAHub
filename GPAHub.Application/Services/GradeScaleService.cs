using AutoMapper;
using FluentValidation;
using GPAHub.Application.Common;
using GPAHub.Application.DTOs.GradeScale;
using GPAHub.Application.Interfaces.Repositories;
using GPAHub.Application.Interfaces.Services;
using GPAHub.Application.Validators;
using GPAHub.Domain.Entities;
using GPAHub.Domain.Exceptions;

namespace GPAHub.Application.Services;

public class GradeScaleService : IGradeScaleService
{
    private readonly IGradeScaleRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly CreateGradeScaleDtoValidator _createValidator;
    private readonly UpdateGradeScaleDtoValidator _updateValidator;
    private readonly SaveGradeDefinitionDtoValidator _definitionValidator;

    public GradeScaleService(
        IGradeScaleRepository repository,
        IUnitOfWork unitOfWork,
        IMapper mapper,
        CreateGradeScaleDtoValidator createValidator,
        UpdateGradeScaleDtoValidator updateValidator,
        SaveGradeDefinitionDtoValidator definitionValidator)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
        _definitionValidator = definitionValidator;
    }

    public async Task<Result<GradeScaleDto>> CreateAsync(Guid studentId, CreateGradeScaleDto dto, CancellationToken cancellationToken = default)
    {
        var validation = await _createValidator.ValidateAsync(dto, cancellationToken);
        if (!validation.IsValid)
        {
            return Result<GradeScaleDto>.Fail(ValidationErrors.From(validation));
        }

        try
        {
            var scale = new GradeScale(dto.Name, studentId, dto.Description, dto.EnforceFullCoverage);

            var existingScales = await _repository.ListByStudentAsync(studentId, cancellationToken) ?? [];
            if (existingScales.Count == 0)
            {
                scale.Activate();
            }

            await _repository.AddAsync(scale, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Result<GradeScaleDto>.Ok(_mapper.Map<GradeScaleDto>(scale));
        }
        catch (DomainException exception)
        {
            return Result<GradeScaleDto>.Fail(DomainResult.ToError(exception));
        }
    }

    public async Task<Result<GradeScaleDto>> UpdateAsync(Guid studentId, Guid scaleId, UpdateGradeScaleDto dto, CancellationToken cancellationToken = default)
    {
        var validation = await _updateValidator.ValidateAsync(dto, cancellationToken);
        if (!validation.IsValid)
        {
            return Result<GradeScaleDto>.Fail(ValidationErrors.From(validation));
        }

        var scale = await RequireScaleAsync(studentId, scaleId, cancellationToken);
        if (scale is null)
        {
            return Result<GradeScaleDto>.Fail(NotFound());
        }

        try
        {
            scale.UpdateDetails(dto.Name, dto.Description, dto.EnforceFullCoverage);

            EnsureValidIfActive(scale);
        }
        catch (DomainException exception)
        {
            return Result<GradeScaleDto>.Fail(DomainResult.ToError(exception));
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<GradeScaleDto>.Ok(_mapper.Map<GradeScaleDto>(scale));
    }

    public async Task<Result> DeleteAsync(Guid studentId, Guid scaleId, CancellationToken cancellationToken = default)
    {
        var scale = await RequireScaleAsync(studentId, scaleId, cancellationToken);
        if (scale is null)
        {
            return Result.Fail(NotFound());
        }

        _repository.Remove(scale);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Ok();
    }

    public async Task<Result<GradeScaleDto>> AddDefinitionAsync(Guid studentId, Guid scaleId, SaveGradeDefinitionDto dto, CancellationToken cancellationToken = default)
    {
        return await MutateDefinitionAsync(studentId, scaleId, dto, cancellationToken,
            scale => scale.AddDefinition(dto.Name, dto.MinMark, dto.MaxMark, dto.Points));
    }

    public async Task<Result<GradeScaleDto>> UpdateDefinitionAsync(Guid studentId, Guid scaleId, Guid definitionId, SaveGradeDefinitionDto dto, CancellationToken cancellationToken = default)
    {
        return await MutateDefinitionAsync(studentId, scaleId, dto, cancellationToken,
            scale => scale.UpdateDefinition(definitionId, dto.Name, dto.MinMark, dto.MaxMark, dto.Points));
    }

    public async Task<Result<GradeScaleDto>> RemoveDefinitionAsync(Guid studentId, Guid scaleId, Guid definitionId, CancellationToken cancellationToken = default)
    {
        var scale = await RequireScaleAsync(studentId, scaleId, cancellationToken);
        if (scale is null)
        {
            return Result<GradeScaleDto>.Fail(NotFound());
        }

        try
        {
            scale.RemoveDefinition(definitionId);
            EnsureValidIfActive(scale);
        }
        catch (DomainException exception)
        {
            return Result<GradeScaleDto>.Fail(DomainResult.ToError(exception));
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<GradeScaleDto>.Ok(_mapper.Map<GradeScaleDto>(scale));
    }

    public async Task<Result<GradeScaleDto>> SetActiveAsync(Guid studentId, Guid scaleId, bool isActive, CancellationToken cancellationToken = default)
    {
        var scale = await RequireScaleAsync(studentId, scaleId, cancellationToken);
        if (scale is null)
        {
            return Result<GradeScaleDto>.Fail(NotFound());
        }

        try
        {
            if (!isActive)
            {
                scale.Deactivate();
            }
            else
            {
                scale.EnsureValid();

                var allScales = await _repository.ListByStudentAsync(studentId, cancellationToken) ?? [];
                foreach (var other in allScales.Where(s => s.Id != scale.Id && s.IsActive))
                {
                    other.Deactivate();
                }

                scale.Activate();
            }
        }
        catch (DomainException exception)
        {
            return Result<GradeScaleDto>.Fail(Error.Validation("scale_not_ready", exception.Message));
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<GradeScaleDto>.Ok(_mapper.Map<GradeScaleDto>(scale));
    }

    public async Task<Result<GradeScaleDto>> GetByIdAsync(Guid studentId, Guid scaleId, CancellationToken cancellationToken = default)
    {
        var scale = await RequireScaleAsync(studentId, scaleId, cancellationToken);
        if (scale is null)
        {
            return Result<GradeScaleDto>.Fail(NotFound());
        }

        return Result<GradeScaleDto>.Ok(_mapper.Map<GradeScaleDto>(scale));
    }

    public async Task<Result<IReadOnlyList<GradeScaleDto>>> ListByStudentAsync(Guid studentId, CancellationToken cancellationToken = default)
    {
        var scales = await _repository.ListByStudentAsync(studentId, cancellationToken);

        return Result<IReadOnlyList<GradeScaleDto>>.Ok(_mapper.Map<List<GradeScaleDto>>(scales));
    }

    public async Task<Result<GradeScaleDto>> GetSystemDefaultAsync(CancellationToken cancellationToken = default)
    {
        var scale = await _repository.GetSystemDefaultAsync(cancellationToken);
        if (scale is null)
        {
            return Result<GradeScaleDto>.Fail(Error.NotFound("default_scale_missing", "System default grade scale is not available."));
        }

        return Result<GradeScaleDto>.Ok(_mapper.Map<GradeScaleDto>(scale));
    }

    private async Task<GradeScale?> RequireScaleAsync(Guid studentId, Guid scaleId, CancellationToken cancellationToken) =>
        await _repository.GetByIdForStudentAsync(scaleId, studentId, cancellationToken);

    private static Error NotFound() => Error.NotFound("scale_not_found", "Grade scale was not found.");

    private static void EnsureValidIfActive(GradeScale scale)
    {
        if (scale.IsActive)
        {
            scale.EnsureValid();
        }
    }

    private async Task<Result<GradeScaleDto>> MutateDefinitionAsync(
        Guid studentId,
        Guid scaleId,
        SaveGradeDefinitionDto dto,
        CancellationToken cancellationToken,
        Action<GradeScale> mutation)
    {
        var validation = await _definitionValidator.ValidateAsync(dto, cancellationToken);
        if (!validation.IsValid)
        {
            return Result<GradeScaleDto>.Fail(ValidationErrors.From(validation));
        }

        var scale = await RequireScaleAsync(studentId, scaleId, cancellationToken);
        if (scale is null)
        {
            return Result<GradeScaleDto>.Fail(NotFound());
        }

        try
        {
            mutation(scale);
            EnsureValidIfActive(scale);
        }
        catch (DomainException exception)
        {
            return Result<GradeScaleDto>.Fail(DomainResult.ToError(exception));
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<GradeScaleDto>.Ok(_mapper.Map<GradeScaleDto>(scale));
    }
}
