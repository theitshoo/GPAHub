using FluentValidation;
using GPAHub.Application.Interfaces.Repositories;
using GPAHub.Application.Interfaces.Services;
using GPAHub.Application.Mappings;
using GPAHub.Application.Services;
using GPAHub.Application.Validators;
using GPAHub.Domain.Entities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Identity;

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

        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IPasswordHasher<Student>, PasswordHasher<Student>>();

        services.AddScoped<CreateGradeScaleDtoValidator>();
        services.AddScoped<UpdateGradeScaleDtoValidator>();
        services.AddScoped<SaveGradeDefinitionDtoValidator>();
        services.AddScoped<CourseInputDtoValidator>();
        services.AddScoped<CalculateGpaRequestDtoValidator>();
        services.AddScoped<TargetPredictionRequestDtoValidator>();
        services.AddScoped<UpdateProfileDtoValidator>();
        services.AddScoped<UpdateBaselineDtoValidator>();
        services.AddScoped<UpgradeToPremiumDtoValidator>();
        services.AddScoped<RegisterStudentDtoValidator>();
        services.AddScoped<LoginRequestDtoValidator>();

        services.AddAutoMapper(typeof(MappingProfile));

        return services;
    }
}
