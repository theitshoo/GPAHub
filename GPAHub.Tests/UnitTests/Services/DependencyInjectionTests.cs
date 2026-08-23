using GPAHub.Application;
using GPAHub.Application.Interfaces.Repositories;
using GPAHub.Application.Interfaces.Services;
using GPAHub.Tests.IntegrationTests;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;

namespace GPAHub.Tests.UnitTests.Services;

public class DependencyInjectionTests
{
    [Fact]
    public void AddApplication_RegistersAllServices()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddScoped(_ => Mock.Of<IStudentRepository>());
        services.AddScoped(_ => Mock.Of<ICourseRepository>());
        services.AddScoped(_ => Mock.Of<IGradeScaleRepository>());
        services.AddScoped(_ => Mock.Of<ISubscriptionRepository>());
        services.AddScoped(_ => Mock.Of<ISemesterRepository>());
        services.AddScoped(_ => Mock.Of<IGpaRecordRepository>());
        services.AddScoped(_ => Mock.Of<ITargetPlanRepository>());
        services.AddScoped(_ => Mock.Of<IRefreshTokenRepository>());
        services.AddScoped(_ => Mock.Of<IPaymentGateway>());
        services.AddScoped(_ => Mock.Of<IPaymentRepository>());
        services.AddScoped(_ => Mock.Of<IUnitOfWork>());
        services.AddScoped(_ => Mock.Of<ITokenService>());

        services.AddApplication();

        var provider = services.BuildServiceProvider();

        Assert.NotNull(provider.GetRequiredService<IGradeScaleService>());
        Assert.NotNull(provider.GetRequiredService<ICourseService>());
        Assert.NotNull(provider.GetRequiredService<IAcademicRecordService>());
        Assert.NotNull(provider.GetRequiredService<IGpaCalculationService>());
        Assert.NotNull(provider.GetRequiredService<ITargetGpaService>());
        Assert.NotNull(provider.GetRequiredService<ISubscriptionService>());
        Assert.NotNull(provider.GetRequiredService<IHistoryService>());
        Assert.NotNull(provider.GetRequiredService<IReportService>());
        Assert.NotNull(provider.GetRequiredService<ISemesterService>());
        Assert.NotNull(provider.GetRequiredService<IAuthService>());
        Assert.NotNull(provider.GetRequiredService<IPaymentService>());
        Assert.NotNull(provider.GetRequiredService<IPremiumActivationService>());
    }
}


