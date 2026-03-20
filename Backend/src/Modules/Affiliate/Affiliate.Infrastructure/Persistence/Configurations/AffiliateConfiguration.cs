namespace Affiliate.Infrastructure.Persistence.Configurations;

public class AffiliateConfiguration : IEntityTypeConfiguration<AffiliateEntity>
{
    public void Configure(EntityTypeBuilder<AffiliateEntity> entity)
    {
        entity.ToTable("affiliates");
        entity.HasKey(e => e.Id);
        entity.Property(e => e.UserId).IsRequired();
        entity.HasIndex(e => e.UserId);
        entity.Property(e => e.Name).HasMaxLength(100).IsRequired();
        entity.Property(e => e.UniqueCode).HasMaxLength(10).IsRequired();
        entity.HasIndex(e => e.UniqueCode).IsUnique();
        entity.Property(e => e.ClickCount).HasDefaultValue(0);
        entity.Property(e => e.CreatedAt).IsRequired();
        entity.Property(e => e.UpdatedAt).IsRequired();
    }
}
