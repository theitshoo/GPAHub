using GPAHub.Application.Interfaces.Repositories;
using GPAHub.Application.Interfaces.Services;
using GPAHub.Application.Services;
using GPAHub.Infrastructure.PdfGeneration;
using GPAHub.Infrastructure.Payments;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace GPAHub.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is not configured.");

        services.AddDbContext<Persistence.GpaHubDbContext>(options => options.UseSqlServer(connectionString));

        services.AddSingleton<IPdfReportGenerator, ReportPdfGenerator>();

        services.Configure<StripeOptions>(configuration.GetSection(StripeOptions.SectionName));
        services.AddHttpClient<IPaymentGateway, StripePaymentProvider>();
        services.AddScoped<IPaymentRepository, Repositories.PaymentRepository>();

        services.AddScoped<IPremiumActivationService, PremiumActivationService>();

        services.AddHostedService<Persistence.RefreshTokenCleanupService>();

        services.AddScoped<IStudentRepository, Repositories.StudentRepository>();
        services.AddScoped<ICourseRepository, Repositories.CourseRepository>();
        services.AddScoped<IGradeScaleRepository, Repositories.GradeScaleRepository>();
        services.AddScoped<ISemesterRepository, Repositories.SemesterRepository>();
        services.AddScoped<ISubscriptionRepository, Repositories.SubscriptionRepository>();
        services.AddScoped<IGpaRecordRepository, Repositories.GpaRecordRepository>();
        services.AddScoped<ITargetPlanRepository, Repositories.TargetPlanRepository>();
        services.AddScoped<IRefreshTokenRepository, Repositories.RefreshTokenRepository>();
        services.AddScoped<IUnitOfWork, Persistence.UnitOfWork>();

        return services;
    }
}
