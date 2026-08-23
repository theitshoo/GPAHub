using AutoMapper;
using GPAHub.Application.Common;
using GPAHub.Application.DTOs.History;
using GPAHub.Application.Interfaces.Repositories;
using GPAHub.Application.Interfaces.Services;

namespace GPAHub.Application.Services;

public class HistoryService : IHistoryService
{
    private const int MaxPageSize = 50;

    private readonly IGpaRecordRepository _recordRepository;
    private readonly ITargetPlanRepository _planRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public HistoryService(
        IGpaRecordRepository recordRepository,
        ITargetPlanRepository planRepository,
        IUnitOfWork unitOfWork,
        IMapper mapper)
    {
        _recordRepository = recordRepository;
        _planRepository = planRepository;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<Result<PagedResultDto<GpaRecordSummaryDto>>> ListGpaRecordsAsync(Guid studentId, HistoryPageRequest pageRequest, CancellationToken cancellationToken = default)
    {
        var (page, pageSize) = NormalizePaging(pageRequest);

        var (items, totalCount) = await _recordRepository.ListByStudentAsync(studentId, page, pageSize, cancellationToken);

        return Result<PagedResultDto<GpaRecordSummaryDto>>.Ok(new PagedResultDto<GpaRecordSummaryDto>(
            _mapper.Map<List<GpaRecordSummaryDto>>(items ?? []),
            page,
            pageSize,
            totalCount));
    }

    public async Task<Result<GpaRecordDetailDto>> GetGpaRecordAsync(Guid studentId, Guid recordId, CancellationToken cancellationToken = default)
    {
        var record = await _recordRepository.GetByIdForStudentAsync(recordId, studentId, cancellationToken);
        if (record is null)
        {
            return Result<GpaRecordDetailDto>.Fail(Error.NotFound("gpa_record_not_found", "GPA record was not found."));
        }

        return Result<GpaRecordDetailDto>.Ok(_mapper.Map<GpaRecordDetailDto>(record));
    }

    public async Task<Result> DeleteGpaRecordAsync(Guid studentId, Guid recordId, CancellationToken cancellationToken = default)
    {
        var record = await _recordRepository.GetByIdForStudentAsync(recordId, studentId, cancellationToken);
        if (record is null)
        {
            return Result.Fail(Error.NotFound("gpa_record_not_found", "GPA record was not found."));
        }

        _recordRepository.Remove(record);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Ok();
    }

    public async Task<Result<PagedResultDto<TargetPlanSummaryDto>>> ListTargetPlansAsync(Guid studentId, HistoryPageRequest pageRequest, CancellationToken cancellationToken = default)
    {
        var (page, pageSize) = NormalizePaging(pageRequest);

        var (items, totalCount) = await _planRepository.ListByStudentAsync(studentId, page, pageSize, cancellationToken);

        return Result<PagedResultDto<TargetPlanSummaryDto>>.Ok(new PagedResultDto<TargetPlanSummaryDto>(
            _mapper.Map<List<TargetPlanSummaryDto>>(items ?? []),
            page,
            pageSize,
            totalCount));
    }

    public async Task<Result<TargetPlanDetailDto>> GetTargetPlanAsync(Guid studentId, Guid planId, CancellationToken cancellationToken = default)
    {
        var plan = await _planRepository.GetByIdForStudentAsync(planId, studentId, cancellationToken);
        if (plan is null)
        {
            return Result<TargetPlanDetailDto>.Fail(Error.NotFound("target_plan_not_found", "Target plan was not found."));
        }

        return Result<TargetPlanDetailDto>.Ok(_mapper.Map<TargetPlanDetailDto>(plan));
    }

    public async Task<Result> DeleteTargetPlanAsync(Guid studentId, Guid planId, CancellationToken cancellationToken = default)
    {
        var plan = await _planRepository.GetByIdForStudentAsync(planId, studentId, cancellationToken);
        if (plan is null)
        {
            return Result.Fail(Error.NotFound("target_plan_not_found", "Target plan was not found."));
        }

        _planRepository.Remove(plan);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Ok();
    }

    private static (int Page, int PageSize) NormalizePaging(HistoryPageRequest request)
    {
        var page = request.Page < 1 ? 1 : request.Page;
        var pageSize = request.PageSize < 1 ? 10 : Math.Min(request.PageSize, MaxPageSize);

        return (page, pageSize);
    }
}
