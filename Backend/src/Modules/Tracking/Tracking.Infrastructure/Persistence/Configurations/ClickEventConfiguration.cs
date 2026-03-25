namespace Tracking.Infrastructure.Persistence.Configurations;

public class ClickEventConfiguration : IEntityTypeConfiguration<ClickEvent>
{
    public void Configure(EntityTypeBuilder<ClickEvent> entity)
    {
        entity.ToTable("click_events");
        entity.HasKey(e => e.Id);
        entity.Property(e => e.AffiliateId).IsRequired();
        entity.Property(e => e.SessionId).HasMaxLength(64).IsRequired();
        entity.Property(e => e.IPAddress).HasMaxLength(45);
        entity.Property(e => e.UserAgent).HasMaxLength(512);
        entity.Property(e => e.ClickedAt).IsRequired();
        entity.Property(e => e.CreatedAt).IsRequired();
        entity.Property(e => e.UpdatedAt).IsRequired();

        // Composite UNIQUE — prevents duplicate clicks at DB level (race-condition safe)
        entity.HasIndex(e => new { e.AffiliateId, e.SessionId }).IsUnique();

        // Index on AffiliateId — speeds up all queries filtering by affiliate
        entity.HasIndex(e => e.AffiliateId);

        // Composite index on (AffiliateId, ClickedAt) — speeds up dashboard time-range queries
        entity.HasIndex(e => new { e.AffiliateId, e.ClickedAt });

        // Index on ClickedAt — speeds up global time-range queries
        entity.HasIndex(e => e.ClickedAt);
    }
}
