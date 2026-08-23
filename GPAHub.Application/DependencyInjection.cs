using FluentValidation;
using GPAHub.Application.Interfaces.Repositories;
using GPAHub.Application.Interfaces.Services;
using GPAHub.Application.Mappings;
using GPAHub.Application.Services;
using GPAHub.Application.Validators;
using Microsoft.Extensions.DependencyInjection;

namespace GPAHub.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IGradeScaleService, GradeScaleService>();
        services.AddScoped<ICourseService, CourseService>();
        services.AddScoped<IAcademicRecordService, AcademicRecordService>();
        services.AddScoped<IGpaCalculationService, GpaCalculationService>();
        services.AddScoped<ITargetGpaService, TargetGpaService>();
        services.AddScoped<ISubscriptionService, SubscriptionService>();
        services.AddScoped<IHistoryService, HistoryService>();
        services.AddScoped<IReportService, ReportService>();

        services.AddScoped<CreateGradeScaleDtoValidator>();
        services.AddScoped<UpdateGradeScaleDtoValidator>();
        services.AddScoped<SaveGradeDefinitionDtoValidator>();
        services.AddScoped<CourseInputDtoValidator>();
        services.AddScoped<CalculateGpaRequestDtoValidator>();
        services.AddScoped<TargetPredictionRequestDtoValidator>();
        services.AddScoped<UpdateProfileDtoValidator>();
        services.AddScoped<UpdateBaselineDtoValidator>();
        services.AddScoped<UpgradeToPremiumDtoValidator>();

        services.AddAutoMapper(typeof(MappingProfile));

        return services;
    }
}
