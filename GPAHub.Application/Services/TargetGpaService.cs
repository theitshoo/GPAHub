using GPAHub.Application.Common;
using GPAHub.Application.DTOs.Target;
using GPAHub.Application.Interfaces.Repositories;
using GPAHub.Application.Interfaces.Services;
using GPAHub.Domain.Constants;
using GPAHub.Domain.DomainServices;
using GPAHub.Domain.Entities;
using GPAHub.Domain.Exceptions;
using GPAHub.Domain.ValueObjects;

namespace GPAHub.Application.Services;

public class TargetGpaService : ITargetGpaService
{
    private readonly IGradeScaleRepository _scaleRepository;
    private readonly ITargetPlanRepository _planRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ISubscriptionService _subscriptionService;

    public TargetGpaService(
        IGradeScaleRepository scaleRepository,
        ITargetPlanRepository planRepository,
        IUnitOfWork unitOfWork,
        ISubscriptionService subscriptionService)
    {
        _scaleRepository = scaleRepository;
        _planRepository = planRepository;
        _unitOfWork = unitOfWork;
        _subscriptionService = subscriptionService;
    }

    public Task<Result<TargetPredictionResponseDto>> PredictAsync(TargetPredictionRequestDto request, Guid? studentId = null, CancellationToken cancellationToken = default) =>
        PredictCoreAsync(request, studentId, saveToHistory: false, cancellationToken);

    public Task<Result<TargetPredictionResponseDto>> PredictAndSaveAsync(Guid studentId, TargetPredictionRequestDto request, CancellationToken cancellationToken = default) =>
        PredictCoreAsync(request, studentId, saveToHistory: true, cancellationToken);

    private async Task<Result<TargetPredictionResponseDto>> PredictCoreAsync(
        TargetPredictionRequestDto request,
        Guid? studentId,
        bool saveToHistory,
        CancellationToken cancellationToken)
    {
        if (request.UpcomingCourses is null || request.UpcomingCourses.Count == 0)
        {
            return Result<TargetPredictionResponseDto>.Fail(
                Error.Validation("upcoming_courses_required", "At least one upcoming course is required."));
        }

        var scaleResult = await ScaleResolver.ResolveAsync(
            _scaleRepository, studentId, request.CustomScaleDefinitions, request.ScaleId, cancellationToken);
        if (scaleResult.IsFailure)
        {
            return Result<TargetPredictionResponseDto>.Fail(scaleResult.Error!);
        }

        var scale = scaleResult.Value;

        GradeCombinationResult? combinationResult = null;
        if (request.IncludeCombinations)
        {
            if (!studentId.HasValue ||
                !await _subscriptionService.IsPremiumAsync(studentId.Value, cancellationToken))
            {
                return Result<TargetPredictionResponseDto>.Fail(Error.Forbidden(
                    "premium_required",
                    "Grade combination generation requires a Premium subscription."));
            }

            var combinations = GenerateCombinations(request, scale);
            if (combinations.IsFailure)
            {
                return Result<TargetPredictionResponseDto>.Fail(combinations.Error!);
            }

            combinationResult = combinations.Value;
        }

        TargetPredictionResult prediction;
        try
        {
            prediction = TargetGpaCalculator.Predict(
                request.CurrentGpa,
                request.CompletedCreditHours,
                request.TargetGpa,
                request.UpcomingCourses.Select(c => new UpcomingCourseInput(c.Name, c.CreditHours)).ToList(),
                maxScaleGpa: scale.GetMaxGpaPoints());
        }
        catch (DomainException exception)
        {
            return Result<TargetPredictionResponseDto>.Fail(
                Error.Validation("prediction_invalid", exception.Message));
        }

        var response = new TargetPredictionResponseDto(
            CurrentQualityPoints: Round(prediction.CurrentQualityPoints),
            TotalFutureCreditHours: prediction.TotalFutureCreditHours,
            TotalCreditHoursAfterCompletion: prediction.TotalCreditHoursAfterCompletion,
            RequiredAverageGpa: Round(prediction.RequiredAverageGpa),
            IsAchievable: prediction.IsAchievable,
            MaxReachableGpa: Round(prediction.MaxReachableGpa),
            UsedScaleName: scale.Name,
            Combinations: combinationResult?
                .Combinations
                .Select(MapCombination)
                .ToList(),
            CombinationsTruncated: combinationResult?.SearchWasTruncated);

        if (!saveToHistory || !studentId.HasValue)
        {
            return Result<TargetPredictionResponseDto>.Ok(response);
        }

        await SavePlanAsync(studentId.Value, request, prediction, cancellationToken);

        return Result<TargetPredictionResponseDto>.Ok(response);
    }

    private static Result<GradeCombinationResult> GenerateCombinations(TargetPredictionRequestDto request, GradeScale scale)
    {
        try
        {
            return Result<GradeCombinationResult>.Ok(GradeCombinationGenerator.Generate(
                currentQualityPoints: request.CurrentGpa * request.CompletedCreditHours,
                completedCreditHours: request.CompletedCreditHours,
                targetGpa: request.TargetGpa,
                upcomingCourses: request.UpcomingCourses
                    .Select(c => new UpcomingCourseInput(c.Name, c.CreditHours))
                    .ToList(),
                availableGrades: scale.Definitions));
        }
        catch (DomainException exception)
        {
            return Result<GradeCombinationResult>.Fail(
                Error.Validation("combinations_invalid", exception.Message));
        }
    }

    private async Task SavePlanAsync(
        Guid studentId,
        TargetPredictionRequestDto request,
        TargetPredictionResult prediction,
        CancellationToken cancellationToken)
    {
        var plan = new TargetPlan(
            studentId,
            request.TargetGpa,
            request.CurrentGpa,
            request.CompletedCreditHours,
            prediction.RequiredAverageGpa,
            prediction.IsAchievable,
            prediction.MaxReachableGpa,
            DateTimeOffset.UtcNow);

        foreach (var course in request.UpcomingCourses)
        {
            plan.AddUpcomingCourse(course.Name, course.CreditHours);
        }

        await _planRepository.AddAsync(plan, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private static GradeCombinationDto MapCombination(GradeCombination combination) =>
        new(
            combination.Assignments
                .Select(a => new GradeCombinationAssignmentDto(a.CourseName, a.GradeName, a.GpaPoints))
                .ToList(),
            Round(combination.ResultingGpa));

    private static decimal Round(decimal value) => new GpaValue(value).Rounded;
}
