using GPAHub.Application.Common;
using GPAHub.Application.DTOs.History;

namespace GPAHub.Application.Interfaces.Services;

public interface IHistoryService
{
    Task<Result<PagedResultDto<GpaRecordSummaryDto>>> ListGpaRecordsAsync(Guid studentId, HistoryPageRequest pageRequest, CancellationToken cancellationToken = default);

    Task<Result<GpaRecordDetailDto>> GetGpaRecordAsync(Guid studentId, Guid recordId, CancellationToken cancellationToken = default);

    Task<Result> DeleteGpaRecordAsync(Guid studentId, Guid recordId, CancellationToken cancellationToken = default);

    Task<Result<PagedResultDto<TargetPlanSummaryDto>>> ListTargetPlansAsync(Guid studentId, HistoryPageRequest pageRequest, CancellationToken cancellationToken = default);

    Task<Result<TargetPlanDetailDto>> GetTargetPlanAsync(Guid studentId, Guid planId, CancellationToken cancellationToken = default);

    Task<Result> DeleteTargetPlanAsync(Guid studentId, Guid planId, CancellationToken cancellationToken = default);
}
