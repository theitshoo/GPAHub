using GPAHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GPAHub.Infrastructure.Persistence.Configurations;

internal static class DecimalPrecision
{
    public const byte StandardPrecision = 18;

    public const byte StandardScale = 6;

    public const byte MoneyPrecision = 18;

    public const byte MoneyScale = 2;
}

public class StudentConfiguration : IEntityTypeConfiguration<Student>
{
    public void Configure(EntityTypeBuilder<Student> builder)
    {
        builder.ToTable("Students");

        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).ValueGeneratedNever();

        builder.Property(s => s.Name)
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(s => s.Email)
            .IsRequired()
            .HasMaxLength(256);

        builder.HasIndex(s => s.Email)
            .IsUnique();

        builder.Property(s => s.PasswordHash)
            .HasMaxLength(500);

        builder.Property(s => s.CurrentGpa)
            .HasPrecision(DecimalPrecision.StandardPrecision, DecimalPrecision.StandardScale);

        builder.Property(s => s.CompletedCreditHours)
            .HasPrecision(DecimalPrecision.StandardPrecision, DecimalPrecision.StandardScale);

        builder.AddRowVersion();
    }
}

