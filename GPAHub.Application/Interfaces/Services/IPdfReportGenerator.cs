using GPAHub.Application.DTOs.Report;

namespace GPAHub.Application.Interfaces.Services;

public interface IPdfReportGenerator
{
    byte[] Generate(ReportDto report);
}
