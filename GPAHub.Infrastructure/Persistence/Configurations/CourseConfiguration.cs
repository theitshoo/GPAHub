using GPAHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GPAHub.Infrastructure.Persistence.Configurations;

internal class SemesterConfiguration : IEntityTypeConfiguration<Semester>
{
    public void Configure(EntityTypeBuilder<Semester> builder)
    {
        builder.ToTable("Semesters");

        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).ValueGeneratedNever();

        builder.Property(s => s.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.HasOne<Student>()
            .WithMany()
            .HasForeignKey(s => s.StudentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(s => s.StudentId);
    }
}

internal class CourseConfiguration : IEntityTypeConfiguration<Course>
{
    public void Configure(EntityTypeBuilder<Course> builder)
    {
        builder.ToTable("Courses");

        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).ValueGeneratedNever();

        builder.Property(c => c.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(c => c.Code)
            .HasMaxLength(50);

        builder.Property(c => c.CreditHours)
            .HasConversion(
                value => value.Value,
                value => new Domain.ValueObjects.CreditHours(value))
            .HasPrecision(5, 2);

        builder.Property(c => c.LetterGrade)
            .HasMaxLength(30);

        builder.Property(c => c.InputType)
            .IsRequired()
            .HasConversion<int>();

        builder.HasOne<Student>()
            .WithMany()
            .HasForeignKey(c => c.StudentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<Semester>()
            .WithMany()
            .HasForeignKey(c => c.SemesterId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasIndex(c => c.StudentId);
        builder.HasIndex(c => c.SemesterId);

        builder.ToTable(t => t.HasCheckConstraint("CK_Courses_CreditHours", "[CreditHours] > 0"));

        builder.ToTable(t => t.HasCheckConstraint("CK_Courses_MarkRange", "[NumericMark] IS NULL OR ([NumericMark] >= 0 AND [NumericMark] <= 100)"));
    }
}
