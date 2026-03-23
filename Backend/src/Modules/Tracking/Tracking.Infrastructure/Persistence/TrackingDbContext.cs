namespace Tracking.Infrastructure.Persistence;

public class TrackingDbContext(DbContextOptions<TrackingDbContext> options) : DbContext(options)
{
    public DbSet<ClickEvent> ClickEvents => Set<ClickEvent>();
    public DbSet<ConversionEvent> ConversionEvents => Set<ConversionEvent>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
        => modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
}
