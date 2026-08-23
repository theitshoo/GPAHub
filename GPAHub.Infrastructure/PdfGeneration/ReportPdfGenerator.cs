using GPAHub.Application.DTOs.Report;
using GPAHub.Application.Interfaces.Services;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace GPAHub.Infrastructure.PdfGeneration;

public class ReportPdfGenerator : IPdfReportGenerator
{
    private const string PrimaryColor = "#1A355E";
    private const string MutedColor = "#666666";

    static ReportPdfGenerator()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public byte[] Generate(ReportDto report)
    {
        return Document.Create(document =>
        {
            document.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(2.2f, Unit.Centimetre);

                page.Header().Column(column =>
                {
                    column.Item().Text(report.Title).FontSize(18).Bold().FontColor(PrimaryColor);
                    column.Item().Text(report.Tagline).FontSize(9).Italic().FontColor(MutedColor);
                    column.Item().PaddingTop(4).LineHorizontal(1).LineColor(PrimaryColor);
                });

                page.Content().PaddingVertical(10).Column(column =>
                {
                    column.Spacing(12);

                    if (report.Baseline is { } baseline)
                    {
                        column.Item().Text("Academic Baseline").FontSize(12).Bold();
                        column.Item().Table(table =>
                        {
                            DefineTwoColumns(table);

                            AddKeyValueRow(table, "Current GPA", baseline.CurrentGpa?.ToString("0.##") ?? "—");
                            AddKeyValueRow(table, "Completed Credit Hours",
                                baseline.CompletedCreditHours?.ToString("0.##") ?? "—");
                        });
                    }

                    if (report.Courses.Count > 0)
                    {
                        column.Item().Text(report.TargetAnalysis is null ? "Courses" : "Planned Courses")
                            .FontSize(12).Bold();

                        column.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.ConstantColumn(24);
                                columns.RelativeColumn();
                                columns.ConstantColumn(55);
                                columns.ConstantColumn(70);
                            });

                            table.Header(header =>
                            {
                                header.Cell().Background(PrimaryColor).Padding(4).Text("#").Bold().FontColor("#FFFFFF");
                                header.Cell().Background(PrimaryColor).Padding(4).Text("Course").Bold().FontColor("#FFFFFF");
                                header.Cell().Background(PrimaryColor).Padding(4).Text("Credits").Bold().FontColor("#FFFFFF");
                                header.Cell().Background(PrimaryColor).Padding(4)
                                    .Text(report.TargetAnalysis is null ? "Grade" : "Status").Bold().FontColor("#FFFFFF");
                            });

                            var index = 1;
                            foreach (var course in report.Courses)
                            {
                                table.Cell().BodyCell($"{index++}");
                                table.Cell().BodyCell(course.Name ?? "Unnamed course");
                                table.Cell().BodyCell(course.CreditHours.ToString("0.##"));
                                table.Cell().BodyCell(
                                    report.TargetAnalysis is null
                                        ? $"{course.GradeName} ({course.GpaPoints:0.##})"
                                        : "planned");
                            }
                        });
                    }

                    if (report.SemesterGpa.HasValue || report.CumulativeGpa.HasValue)
                    {
                        column.Item().Text("Results").FontSize(12).Bold();
                        column.Item().Table(table =>
                        {
                            DefineTwoColumns(table);

                            if (report.SemesterGpa.HasValue)
                            {
                                AddKeyValueRow(table, "Semester GPA", report.SemesterGpa.Value.ToString("0.00"));
                            }

                            if (report.CumulativeGpa.HasValue)
                            {
                                AddKeyValueRow(table, "Cumulative GPA", report.CumulativeGpa.Value.ToString("0.00"));
                            }
                        });
                    }

                    if (report.TargetAnalysis is { } target)
                    {
                        column.Item().Text("Target Analysis").FontSize(12).Bold();
                        column.Item().Table(table =>
                        {
                            DefineTwoColumns(table);

                            AddKeyValueRow(table, "Target GPA", target.TargetGpa.ToString("0.00"));
                            AddKeyValueRow(table, "Required Average", target.RequiredAverageGpa.ToString("0.00"));
                            AddKeyValueRow(table, "Feasibility",
                                target.IsAchievable ? "Achievable" : "Not achievable");

                            if (target.MaxReachableGpa.HasValue)
                            {
                                AddKeyValueRow(table, "Maximum Reachable GPA",
                                    target.MaxReachableGpa.Value.ToString("0.00"));
                            }
                        });
                    }
                });

                page.Footer().AlignCenter().Text(text =>
                {
                    text.Span($"Generated {report.GeneratedAtUtc:yyyy-MM-dd HH:mm} UTC  ·  ");
                    text.Span(report.Tagline).Italic().FontColor("#999999");
                });
            });
        }).GeneratePdf();
    }

    private static void DefineTwoColumns(TableDescriptor table)
    {
        table.ColumnsDefinition(columns =>
        {
            columns.RelativeColumn(2);
            columns.RelativeColumn(3);
        });
    }

    private static void AddKeyValueRow(TableDescriptor table, string label, string value)
    {
        table.Cell().BorderBottom(0.5f).BorderColor("#DDDDDD").Padding(4).Text(label).SemiBold();
        table.Cell().BorderBottom(0.5f).BorderColor("#DDDDDD").Padding(4).Text(value);
    }
}

internal static class PdfTableExtensions
{
    public static void BodyCell(this IContainer container, string text) =>
        container.BorderBottom(0.5f).BorderColor("#DDDDDD").Padding(4).Text(text);
}
