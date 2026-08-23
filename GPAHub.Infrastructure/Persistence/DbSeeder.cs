using GPAHub.Domain.Constants;
using GPAHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GPAHub.Infrastructure.Persistence;

public static class DbSeeder
{
    public const string SystemDefaultScaleName = "Standard Scale";

    public static async Task SeedAsync(GpaHubDbContext context)
    {
        await EnsurePlansAsync(context);
        await EnsureSystemDefaultScaleAsync(context);
        await context.SaveChangesAsync();
    }

    private static async Task EnsurePlansAsync(GpaHubDbContext context)
    {
        if (!await context.Plans.AnyAsync(p => p.Name == Plan.FreeName))
        {
            context.Plans.Add(Plan.Free());
        }

        if (!await context.Plans.AnyAsync(p => p.Name == Plan.PremiumName))
        {
            context.Plans.Add(Plan.Premium());
        }
    }

    private static async Task EnsureSystemDefaultScaleAsync(GpaHubDbContext context)
    {
        var exists = await context.GradeScales.AnyAsync(s => s.StudentId == null);

        if (exists)
        {
            return;
        }

        var scale = new GradeScale(SystemDefaultScaleName, studentId: null,
            description: "Default grading scale provided by GPAHub.");

        scale.AddDefinition("A", 90, 100, 4m);
        scale.AddDefinition("B", 80, 89, 3m);
        scale.AddDefinition("C", 70, 79, 2m);
        scale.AddDefinition("D", 60, 69, 1m);
        scale.AddDefinition("F", 0, 59, 0m);

        scale.EnsureValid();
        scale.Activate();

        context.GradeScales.Add(scale);
    }
}
