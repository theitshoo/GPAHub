using GPAHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GPAHub.Infrastructure.Persistence.Configurations;

internal class GpaRecordConfiguration : IEntityTypeConfiguration<GpaRecord>
{
    public void Configure(EntityTypeBuilder<GpaRecord> builder)
    {
        builder.ToTable("GpaRecords");

        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).ValueGeneratedNever();

        builder.Property(r => r.SemesterGpa)
            .HasPrecision(DecimalPrecision.StandardPrecision, DecimalPrecision.StandardScale);

        builder.Property(r => r.CumulativeGpa)
            .HasPrecision(DecimalPrecision.StandardPrecision, DecimalPrecision.StandardScale);

        builder.Property(r => r.TotalCreditHours)
            .HasPrecision(DecimalPrecision.StandardPrecision, DecimalPrecision.StandardScale);

        builder.Property(r => r.TotalQualityPoints)
            .HasPrecision(DecimalPrecision.StandardPrecision, DecimalPrecision.StandardScale);

        builder.Property(r => r.CalculationType)
            .IsRequired()
            .HasConversion<int>();

        builder.HasOne<Student>()
            .WithMany()
            .HasForeignKey(r => r.StudentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(r => new { r.StudentId, r.CreatedAtUtc });

        builder.HasMany(r => r.CourseLines)
            .WithOne()
            .HasForeignKey(l => l.GpaRecordId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

internal class GpaRecordCourseLineConfiguration : IEntityTypeConfiguration<GpaRecordCourseLine>
{
    public void Configure(EntityTypeBuilder<GpaRecordCourseLine> builder)
    {
        builder.ToTable("GpaRecordCourseLines");

        builder.HasKey(l => l.Id);
        builder.Property(l => l.Id).ValueGeneratedNever();

        builder.Property(l => l.CourseName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(l => l.CourseCode)
            .HasMaxLength(50);

        builder.Property(l => l.GradeName)
            .IsRequired()
            .HasMaxLength(30);

        builder.Property(l => l.CreditHours)
            .HasPrecision(5, 2);

        builder.Property(l => l.GpaPoints)
            .HasPrecision(DecimalPrecision.StandardPrecision, DecimalPrecision.StandardScale);

        builder.Property(l => l.QualityPoints)
            .HasPrecision(DecimalPrecision.StandardPrecision, DecimalPrecision.StandardScale);
    }
}

internal class TargetPlanConfiguration : IEntityTypeConfiguration<TargetPlan>
{
    public void Configure(EntityTypeBuilder<TargetPlan> builder)
    {
        builder.ToTable("TargetPlans");

        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).ValueGeneratedNever();

        builder.Property(p => p.TargetGpa)
            .HasPrecision(DecimalPrecision.StandardPrecision, DecimalPrecision.StandardScale);

        builder.Property(p => p.CurrentGpa)
            .HasPrecision(DecimalPrecision.StandardPrecision, DecimalPrecision.StandardScale);

        builder.Property(p => p.CompletedCreditHours)
            .HasPrecision(DecimalPrecision.StandardPrecision, DecimalPrecision.StandardScale);

        builder.Property(p => p.RequiredAverageGpa)
            .HasPrecision(DecimalPrecision.StandardPrecision, DecimalPrecision.StandardScale);

        builder.Property(p => p.MaxReachableGpa)
            .HasPrecision(DecimalPrecision.StandardPrecision, DecimalPrecision.StandardScale);

        builder.HasOne<Student>()
            .WithMany()
            .HasForeignKey(p => p.StudentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(p => new { p.StudentId, p.CreatedAtUtc });

        builder.HasMany(p => p.UpcomingCourses)
            .WithOne()
            .HasForeignKey(c => c.TargetPlanId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

internal class TargetPlanUpcomingCourseConfiguration : IEntityTypeConfiguration<TargetPlanUpcomingCourse>
{
    public void Configure(EntityTypeBuilder<TargetPlanUpcomingCourse> builder)
    {
        builder.ToTable("TargetPlanUpcomingCourses");

        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).ValueGeneratedNever();

        builder.Property(c => c.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(c => c.CreditHours)
            .HasPrecision(5, 2);
    }
}
