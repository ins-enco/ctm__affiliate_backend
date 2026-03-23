namespace Tracking.Infrastructure.Persistence.Configurations;

public class ConversionEventConfiguration : IEntityTypeConfiguration<ConversionEvent>
{
    public void Configure(EntityTypeBuilder<ConversionEvent> entity)
    {
        entity.ToTable("conversion_events");
        entity.HasKey(e => e.Id);
        entity.Property(e => e.AffiliateId).IsRequired();
        entity.Property(e => e.SessionId).HasMaxLength(64).IsRequired();
        entity.Property(e => e.UserId);
        entity.Property(e => e.ConversionType).HasMaxLength(50).IsRequired();
        entity.Property(e => e.ConvertedAt).IsRequired();
        entity.Property(e => e.CreatedAt).IsRequired();
        entity.Property(e => e.UpdatedAt).IsRequired();

        // Prevent duplicate conversions of the same type for the same session
        entity.HasIndex(e => new { e.SessionId, e.ConversionType }).IsUnique();

        // For commission aggregation queries
        entity.HasIndex(e => new { e.AffiliateId, e.ConversionType });

        // For time-range reporting
        entity.HasIndex(e => e.ConvertedAt);

        // For click attribution lookup
        entity.HasIndex(e => e.SessionId);
    }
}
