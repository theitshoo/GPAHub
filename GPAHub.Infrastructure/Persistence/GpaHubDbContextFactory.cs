using GPAHub.Application.Interfaces.Repositories;
using GPAHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GPAHub.Infrastructure.Persistence;

public class GpaHubDbContextFactory : Microsoft.EntityFrameworkCore.Design.IDesignTimeDbContextFactory<GpaHubDbContext>
{
    public const string DesignTimeConnectionString =
        @"Server=(localdb)\MSSQLLocalDB;Database=GPAHub_DesignTime;Trusted_Connection=True;TrustServerCertificate=True";

    public GpaHubDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<GpaHubDbContext>()
            .UseSqlServer(DesignTimeConnectionString)
            .Options;

        return new GpaHubDbContext(options);
    }
}
