namespace Affiliate.Infrastructure.Persistence;

public class AffiliateDbContext(DbContextOptions<AffiliateDbContext> options) : DbContext(options)
{
    public DbSet<AffiliateEntity> Affiliates => Set<AffiliateEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
        => modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
}
