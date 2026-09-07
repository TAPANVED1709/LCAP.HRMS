using LCAP.HRMS.Domain.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LCAP.HRMS.Infrastructure.Persistence.Configurations;

public sealed class BaseEntityConfiguration<T> : IEntityTypeConfiguration<T> where T : BaseEntity
{
    public void Configure(EntityTypeBuilder<T> builder)
    {
        builder.HasKey(entity => entity.Id);
        builder.Property(entity => entity.Id).ValueGeneratedNever();
        builder.Property(entity => entity.CreatedAt).HasPrecision(7).IsRequired();
        builder.Property(entity => entity.CreatedBy).HasMaxLength(200);
        builder.Property(entity => entity.UpdatedAt).HasPrecision(7);
        builder.Property(entity => entity.UpdatedBy).HasMaxLength(200);
        builder.Property(entity => entity.IsDeleted).HasDefaultValue(false).IsRequired();
        builder.HasQueryFilter(entity => !entity.IsDeleted);
    }
}
