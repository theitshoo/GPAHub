using AutoMapper;
using FluentValidation;
using GPAHub.Application.Common;
using GPAHub.Application.DTOs.Course;
using GPAHub.Application.DTOs.Semester;
using GPAHub.Application.Interfaces.Repositories;
using GPAHub.Application.Interfaces.Services;
using GPAHub.Application.Validators;
using GPAHub.Domain.Entities;

namespace GPAHub.Application.Services;

public class SemesterService : ISemesterService
{
    private readonly ISemesterRepository _semesterRepository;
    private readonly ICourseRepository _courseRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly CreateSemesterDtoValidator _createValidator = new();

    public SemesterService(
        ISemesterRepository semesterRepository,
        ICourseRepository courseRepository,
        IUnitOfWork unitOfWork,
        IMapper mapper,
        CreateSemesterDtoValidator createValidator)
    {
        _semesterRepository = semesterRepository;
        _courseRepository = courseRepository;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _createValidator = createValidator;
    }

    public async Task<Result<SemesterOptionDto>> CreateAsync(Guid studentId, CreateSemesterDto dto, CancellationToken cancellationToken = default)
    {
        var validation = await _createValidator.ValidateAsync(dto, cancellationToken);
        if (!validation.IsValid)
        {
            return Result<SemesterOptionDto>.Fail(ValidationErrors.From(validation));
        }

        var semester = new Semester(studentId, dto.Name);

        await _semesterRepository.AddAsync(semester, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<SemesterOptionDto>.Ok(_mapper.Map<SemesterOptionDto>(semester));
    }

    public async Task<Result<IReadOnlyList<SemesterOptionDto>>> ListByStudentAsync(Guid studentId, CancellationToken cancellationToken = default)
    {
        var semesters = await _semesterRepository.ListByStudentAsync(studentId, cancellationToken) ?? [];

        return Result<IReadOnlyList<SemesterOptionDto>>.Ok(_mapper.Map<List<SemesterOptionDto>>(semesters));
    }

    public async Task<Result> RenameAsync(Guid studentId, Guid semesterId, UpdateSemesterDto dto, CancellationToken cancellationToken = default)
    {
        var semester = await RequireSemesterAsync(studentId, semesterId, cancellationToken);
        if (semester is null)
        {
            return Result.Fail(NotFound());
        }

        try
        {
            semester.Rename(dto.Name);
        }
        catch (Domain.Exceptions.DomainException exception)
        {
            return Result.Fail(DomainResult.ToError(exception));
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Ok();
    }

    public async Task<Result> DeleteAsync(Guid studentId, Guid semesterId, CancellationToken cancellationToken = default)
    {
        var semester = await RequireSemesterAsync(studentId, semesterId, cancellationToken);
        if (semester is null)
        {
            return Result.Fail(NotFound());
        }

        var attachedCourses = await _courseRepository.ListBySemesterTrackedAsync(studentId, semesterId, cancellationToken);
        foreach (var course in attachedCourses)
        {
            course.RemoveFromSemester();
        }

        _semesterRepository.Remove(semester);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Ok();
    }

    private async Task<Semester?> RequireSemesterAsync(Guid studentId, Guid semesterId, CancellationToken cancellationToken) =>
        await _semesterRepository.GetByIdForStudentAsync(semesterId, studentId, cancellationToken);

    private static Error NotFound() => Error.NotFound("semester_not_found", "Semester was not found.");
}
