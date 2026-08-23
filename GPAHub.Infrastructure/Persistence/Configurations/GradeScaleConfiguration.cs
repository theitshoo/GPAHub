using GPAHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GPAHub.Infrastructure.Persistence.Configurations;

internal class GradeScaleConfiguration : IEntityTypeConfiguration<GradeScale>
{
    public void Configure(EntityTypeBuilder<GradeScale> builder)
    {
        builder.ToTable("GradeScales");

        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).ValueGeneratedNever();

        builder.Property(s => s.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(s => s.Description)
            .HasMaxLength(500);

        builder.HasOne<Student>()
            .WithMany()
            .HasForeignKey(s => s.StudentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(s => new { s.StudentId, s.Name })
            .IsUnique()
            .HasFilter("[StudentId] IS NOT NULL");

        builder.HasIndex(s => s.StudentId)
            .IsUnique()
            .HasFilter("[IsActive] = 1 AND [StudentId] IS NOT NULL");

        builder.HasMany(s => s.Definitions)
            .WithOne()
            .HasForeignKey(d => d.GradeScaleId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

internal class GradeDefinitionConfiguration : IEntityTypeConfiguration<GradeDefinition>
{
    public void Configure(EntityTypeBuilder<GradeDefinition> builder)
    {
        builder.ToTable("GradeDefinitions");

        builder.HasKey(d => d.Id);
        builder.Property(d => d.Id).ValueGeneratedNever();

        builder.Property(d => d.Name)
            .IsRequired()
            .HasMaxLength(30);

        builder.Property(d => d.Points)
            .HasPrecision(DecimalPrecision.StandardPrecision, DecimalPrecision.StandardScale);

        builder.HasIndex(d => new { d.GradeScaleId, d.Name })
            .IsUnique();

        builder.ToTable(t => t.HasCheckConstraint("CK_GradeDefinitions_MarkRange", "[MinMark] <= [MaxMark] AND [MinMark] >= 0 AND [MaxMark] <= 100"));

        builder.ToTable(t => t.HasCheckConstraint("CK_GradeDefinitions_Points", "[Points] >= 0"));
    }
}
