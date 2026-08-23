using GPAHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GPAHub.Infrastructure.Persistence;

public class GpaHubDbContext : DbContext
{
    public GpaHubDbContext(DbContextOptions<GpaHubDbContext> options) : base(options)
    {
    }

    public DbSet<Student> Students => Set<Student>();

    public DbSet<GradeScale> GradeScales => Set<GradeScale>();

    public DbSet<GradeDefinition> GradeDefinitions => Set<GradeDefinition>();

    public DbSet<Semester> Semesters => Set<Semester>();

    public DbSet<Course> Courses => Set<Course>();

    public DbSet<Subscription> Subscriptions => Set<Subscription>();

    public DbSet<Payment> Payments => Set<Payment>();

    public DbSet<Plan> Plans => Set<Plan>();

    public DbSet<GpaRecord> GpaRecords => Set<GpaRecord>();

    public DbSet<TargetPlan> TargetPlans => Set<TargetPlan>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(GpaHubDbContext).Assembly);
    }
}
