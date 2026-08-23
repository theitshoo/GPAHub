using AutoMapper;
using FluentValidation;
using GPAHub.Application.Common;
using GPAHub.Application.DTOs.Student;
using GPAHub.Application.Interfaces.Repositories;
using GPAHub.Application.Interfaces.Services;
using GPAHub.Application.Validators;
using GPAHub.Domain.Entities;
using GPAHub.Domain.Exceptions;

namespace GPAHub.Application.Services;

public class AcademicRecordService : IAcademicRecordService
{
    private readonly IStudentRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly UpdateProfileDtoValidator _profileValidator;
    private readonly UpdateBaselineDtoValidator _baselineValidator;

    public AcademicRecordService(
        IStudentRepository repository,
        IUnitOfWork unitOfWork,
        IMapper mapper,
        UpdateProfileDtoValidator profileValidator,
        UpdateBaselineDtoValidator baselineValidator)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _profileValidator = profileValidator;
        _baselineValidator = baselineValidator;
    }

    public async Task<Result<StudentProfileDto>> GetProfileAsync(Guid studentId, CancellationToken cancellationToken = default)
    {
        var student = await RequireStudentAsync(studentId, cancellationToken);
        if (student is null)
        {
            return Result<StudentProfileDto>.Fail(NotFound());
        }

        return Result<StudentProfileDto>.Ok(_mapper.Map<StudentProfileDto>(student));
    }

    public async Task<Result<StudentProfileDto>> UpdateProfileAsync(Guid studentId, UpdateProfileDto dto, CancellationToken cancellationToken = default)
    {
        var validation = await _profileValidator.ValidateAsync(dto, cancellationToken);
        if (!validation.IsValid)
        {
            return Result<StudentProfileDto>.Fail(ValidationErrors.From(validation));
        }

        var student = await RequireStudentAsync(studentId, cancellationToken);
        if (student is null)
        {
            return Result<StudentProfileDto>.Fail(NotFound());
        }

        student.Rename(dto.Name);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<StudentProfileDto>.Ok(_mapper.Map<StudentProfileDto>(student));
    }

    public async Task<Result<AcademicBaselineDto>> GetBaselineAsync(Guid studentId, CancellationToken cancellationToken = default)
    {
        var student = await RequireStudentAsync(studentId, cancellationToken);
        if (student is null)
        {
            return Result<AcademicBaselineDto>.Fail(NotFound());
        }

        return Result<AcademicBaselineDto>.Ok(new AcademicBaselineDto(student.CurrentGpa, student.CompletedCreditHours));
    }

    public async Task<Result<AcademicBaselineDto>> UpdateBaselineAsync(Guid studentId, UpdateBaselineDto dto, CancellationToken cancellationToken = default)
    {
        var validation = await _baselineValidator.ValidateAsync(dto, cancellationToken);
        if (!validation.IsValid)
        {
            return Result<AcademicBaselineDto>.Fail(ValidationErrors.From(validation));
        }

        var student = await RequireStudentAsync(studentId, cancellationToken);
        if (student is null)
        {
            return Result<AcademicBaselineDto>.Fail(NotFound());
        }

        try
        {
            student.UpdateBaseline(dto.CurrentGpa, dto.CompletedCreditHours);
        }
        catch (DomainException exception)
        {
            return Result<AcademicBaselineDto>.Fail(DomainResult.ToError(exception));
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<AcademicBaselineDto>.Ok(new AcademicBaselineDto(student.CurrentGpa, student.CompletedCreditHours));
    }

    public async Task<Result> ClearBaselineAsync(Guid studentId, CancellationToken cancellationToken = default)
    {
        var student = await RequireStudentAsync(studentId, cancellationToken);
        if (student is null)
        {
            return Result.Fail(NotFound());
        }

        student.ClearBaseline();
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Ok();
    }

    private async Task<Student?> RequireStudentAsync(Guid studentId, CancellationToken cancellationToken) =>
        await _repository.GetByIdAsync(studentId, cancellationToken);

    private static Error NotFound() => Error.NotFound("student_not_found", "Student was not found.");
}
