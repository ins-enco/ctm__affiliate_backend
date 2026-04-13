namespace Auth.Infrastructure.Persistence.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> entity)
    {
        entity.ToTable("users");
        entity.HasKey(e => e.Id);
        entity.Property(e => e.Email).HasMaxLength(255).IsRequired();
        entity.HasIndex(e => e.Email).IsUnique();
        entity.Property(e => e.PasswordHash).HasMaxLength(255).IsRequired();
        entity.Property(e => e.IsEmailVerified).IsRequired().HasDefaultValue(false);
        entity.Property(e => e.CreatedAt).IsRequired();
        entity.Property(e => e.UpdatedAt).IsRequired();
    }
}
