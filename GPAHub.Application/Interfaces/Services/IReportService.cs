using GPAHub.Application.Common;
using GPAHub.Application.DTOs.Report;

namespace GPAHub.Application.Interfaces.Services;

public interface IReportService
{
    Task<Result<ReportDto>> BuildGpaReportAsync(Guid studentId, Guid recordId, CancellationToken cancellationToken = default);

    Task<Result<ReportDto>> BuildTargetReportAsync(Guid studentId, Guid planId, CancellationToken cancellationToken = default);
}
