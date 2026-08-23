using GPAHub.Application.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace GPAHub.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<Persistence.GpaHubDbContext>(options => options.UseSqlServer(connectionString));

        services.AddScoped<IStudentRepository, Repositories.StudentRepository>();
        services.AddScoped<ICourseRepository, Repositories.CourseRepository>();
        services.AddScoped<IGradeScaleRepository, Repositories.GradeScaleRepository>();
        services.AddScoped<ISemesterRepository, Repositories.SemesterRepository>();
        services.AddScoped<ISubscriptionRepository, Repositories.SubscriptionRepository>();
        services.AddScoped<IGpaRecordRepository, Repositories.GpaRecordRepository>();
        services.AddScoped<ITargetPlanRepository, Repositories.TargetPlanRepository>();
        services.AddScoped<IUnitOfWork, Persistence.UnitOfWork>();

        return services;
    }
}
