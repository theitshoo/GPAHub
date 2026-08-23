using AutoMapper;
using FluentValidation;
using GPAHub.Application.Common;
using GPAHub.Application.DTOs.Course;
using GPAHub.Application.DTOs.Student;
using GPAHub.Application.Interfaces.Repositories;
using GPAHub.Application.Interfaces.Services;
using GPAHub.Application.Validators;
using GPAHub.Domain.Entities;
using GPAHub.Domain.Enums;
using GPAHub.Domain.Exceptions;

namespace GPAHub.Application.Services;

public class CourseService : ICourseService
{
    private readonly ICourseRepository _repository;
    private readonly ISemesterRepository _semesterRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly CourseInputDtoValidator _validator;

    public CourseService(ICourseRepository repository, ISemesterRepository semesterRepository, IUnitOfWork unitOfWork, IMapper mapper, CourseInputDtoValidator validator)
    {
        _repository = repository;
        _semesterRepository = semesterRepository;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _validator = validator;
    }

    public async Task<Result<CourseDto>> CreateAsync(Guid studentId, CourseInputDto dto, CancellationToken cancellationToken = default)
    {
        var validation = await _validator.ValidateAsync(dto, cancellationToken);
        if (!validation.IsValid)
        {
            return Result<CourseDto>.Fail(ValidationErrors.From(validation));
        }

        try
        {
            if (dto.SemesterId.HasValue &&
                await RequireSemesterAsync(studentId, dto.SemesterId.Value, cancellationToken) is null)
            {
                return Result<CourseDto>.Fail(SemesterNotFound());
            }

            var course = BuildCourse(studentId, dto);

            if (dto.SemesterId.HasValue)
            {
                course.AssignToSemester(dto.SemesterId.Value);
            }

            await _repository.AddAsync(course, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Result<CourseDto>.Ok(_mapper.Map<CourseDto>(course));
        }
        catch (DomainException exception)
        {
            return Result<CourseDto>.Fail(DomainResult.ToError(exception));
        }
    }

    public async Task<Result<CourseDto>> UpdateAsync(Guid studentId, Guid courseId, CourseInputDto dto, CancellationToken cancellationToken = default)
    {
        var validation = await _validator.ValidateAsync(dto, cancellationToken);
        if (!validation.IsValid)
        {
            return Result<CourseDto>.Fail(ValidationErrors.From(validation));
        }

        var course = await _repository.GetByIdForStudentAsync(courseId, studentId, cancellationToken);
        if (course is null)
        {
            return Result<CourseDto>.Fail(Error.NotFound("course_not_found", "Course was not found."));
        }

        try
        {
            if (dto.SemesterId.HasValue &&
                await RequireSemesterAsync(studentId, dto.SemesterId.Value, cancellationToken) is null)
            {
                return Result<CourseDto>.Fail(SemesterNotFound());
            }

            course.UpdateDetails(dto.Name, dto.Code, dto.CreditHours);

            if (dto.InputType == GradeInputType.NumericMark)
            {
                course.UpdateAsNumeric(dto.NumericMark!.Value);
            }
            else
            {
                course.UpdateAsLetter(dto.LetterGrade!);
            }

            if (dto.SemesterId.HasValue)
            {
                await RequireSemesterAsync(studentId, dto.SemesterId.Value, cancellationToken);
                course.AssignToSemester(dto.SemesterId.Value);
            }
            else
            {
                course.RemoveFromSemester();
            }
        }
        catch (DomainException exception)
        {
            return Result<CourseDto>.Fail(DomainResult.ToError(exception));
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<CourseDto>.Ok(_mapper.Map<CourseDto>(course));
    }

    public async Task<Result> DeleteAsync(Guid studentId, Guid courseId, CancellationToken cancellationToken = default)
    {
        var course = await _repository.GetByIdForStudentAsync(courseId, studentId, cancellationToken);
        if (course is null)
        {
            return Result.Fail(Error.NotFound("course_not_found", "Course was not found."));
        }

        _repository.Remove(course);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Ok();
    }

    public async Task<Result<CourseDto>> GetByIdAsync(Guid studentId, Guid courseId, CancellationToken cancellationToken = default)
    {
        var course = await _repository.GetByIdForStudentAsync(courseId, studentId, cancellationToken);
        if (course is null)
        {
            return Result<CourseDto>.Fail(Error.NotFound("course_not_found", "Course was not found."));
        }

        return Result<CourseDto>.Ok(_mapper.Map<CourseDto>(course));
    }

    public async Task<Result<IReadOnlyList<CourseDto>>> ListByStudentAsync(Guid studentId, Guid? semesterId = null, CancellationToken cancellationToken = default)
    {
        var courses = await _repository.ListByStudentAsync(studentId, semesterId, cancellationToken) ?? [];

        return Result<IReadOnlyList<CourseDto>>.Ok(_mapper.Map<List<CourseDto>>(courses));
    }

    private async Task<Semester?> RequireSemesterAsync(Guid studentId, Guid semesterId, CancellationToken cancellationToken) =>
        await _semesterRepository.GetByIdForStudentAsync(semesterId, studentId, cancellationToken);

    private static Error SemesterNotFound() => Error.NotFound("semester_not_found", "Semester was not found for this student.");

    private static Course BuildCourse(Guid studentId, CourseInputDto dto) =>
        dto.InputType == GradeInputType.NumericMark
            ? Course.CreateNumeric(studentId, dto.Name, dto.Code, dto.CreditHours, dto.NumericMark!.Value)
            : Course.CreateLetterGrade(studentId, dto.Name, dto.Code, dto.CreditHours, dto.LetterGrade!);
}

