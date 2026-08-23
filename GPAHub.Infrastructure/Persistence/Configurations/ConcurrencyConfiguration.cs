using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GPAHub.Infrastructure.Persistence.Configurations;

internal static class ConcurrencyConfiguration
{
    public static void AddRowVersion(this EntityTypeBuilder builder)
    {
        builder.Property<byte[]>("Version")
            .IsRowVersion()
            .IsRequired(false);
    }
}
