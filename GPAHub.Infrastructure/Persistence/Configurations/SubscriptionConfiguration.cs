using GPAHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GPAHub.Infrastructure.Persistence.Configurations;

internal class SubscriptionConfiguration : IEntityTypeConfiguration<Subscription>
{
    public void Configure(EntityTypeBuilder<Subscription> builder)
    {
        builder.ToTable("Subscriptions");

        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).ValueGeneratedNever();

        builder.Property(s => s.Type)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(s => s.Status)
            .IsRequired()
            .HasConversion<int>();

        builder.HasOne<Student>()
            .WithMany()
            .HasForeignKey(s => s.StudentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(s => s.StudentId);

        builder.AddRowVersion();

        builder.ToTable(t => t.HasCheckConstraint(
            "CK_Subscriptions_DateRange",
            "[EndDate] IS NULL OR [EndDate] >= [StartDate]"));
    }
}

internal class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> builder)
    {
        builder.ToTable("Payments");

        builder.AddRowVersion();

        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).ValueGeneratedNever();

        builder.Property(p => p.Amount)
            .HasPrecision(DecimalPrecision.MoneyPrecision, DecimalPrecision.MoneyScale);

        builder.Property(p => p.Currency)
            .IsRequired()
            .HasMaxLength(3)
            .IsFixedLength();

        builder.Property(p => p.ExternalReference)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(p => p.Status)
            .IsRequired()
            .HasConversion<int>();

        builder.HasOne<Subscription>()
            .WithMany(s => s.Payments)
            .HasForeignKey(p => p.SubscriptionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(p => p.ExternalReference)
            .IsUnique();

        builder.ToTable(t => t.HasCheckConstraint("CK_Payments_Amount", "[Amount] >= 0"));
    }
}

internal class PlanConfiguration : IEntityTypeConfiguration<Plan>
{
    public void Configure(EntityTypeBuilder<Plan> builder)
    {
        builder.ToTable("Plans");

        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).ValueGeneratedNever();

        builder.Property(p => p.Name)
            .IsRequired()
            .HasMaxLength(50);

        builder.HasIndex(p => p.Name)
            .IsUnique();

        builder.Property(p => p.Features)
            .HasConversion(
                features => string.Join(';', features),
                value => (IReadOnlyCollection<string>)value.Split(';', StringSplitOptions.RemoveEmptyEntries).ToList())
            .HasMaxLength(2000);
    }
}


