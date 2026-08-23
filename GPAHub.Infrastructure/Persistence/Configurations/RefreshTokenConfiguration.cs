using GPAHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GPAHub.Infrastructure.Persistence.Configurations;

internal class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("RefreshTokens");

        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id).ValueGeneratedNever();

        builder.Property(t => t.TokenHash)
            .IsRequired()
            .HasMaxLength(128);

        builder.HasOne<Student>()
            .WithMany()
            .HasForeignKey(t => t.StudentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(t => t.TokenHash)
            .IsUnique();

        builder.HasIndex(t => t.StudentId);
    }
}
