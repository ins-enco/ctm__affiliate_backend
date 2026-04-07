namespace Auth.Infrastructure.Persistence.Configurations;

public class EmailVerificationTokenConfiguration : IEntityTypeConfiguration<EmailVerificationToken>
{
    public void Configure(EntityTypeBuilder<EmailVerificationToken> entity)
    {
        entity.ToTable("email_verification_tokens");
        entity.HasKey(e => e.Id);
        entity.Property(e => e.Email).HasMaxLength(256).IsRequired();
        entity.Property(e => e.Token).HasMaxLength(128).IsRequired();
        entity.HasIndex(e => e.Token).IsUnique();
        entity.HasIndex(e => new { e.UserId, e.ConsumedAt });
        entity.Property(e => e.ExpiresAt).IsRequired();
        entity.Property(e => e.CreatedAt).IsRequired();
        entity.Property(e => e.UpdatedAt).IsRequired();
        entity.HasOne(e => e.User)
              .WithMany(u => u.VerificationTokens)
              .HasForeignKey(e => e.UserId)
              .OnDelete(DeleteBehavior.Cascade);
    }
}
