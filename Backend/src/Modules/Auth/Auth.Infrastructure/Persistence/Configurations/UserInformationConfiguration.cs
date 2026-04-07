namespace Auth.Infrastructure.Persistence.Configurations;

public class UserInformationConfiguration : IEntityTypeConfiguration<UserInformation>
{
    public void Configure(EntityTypeBuilder<UserInformation> entity)
    {
        entity.ToTable("user_information");
        entity.HasKey(e => e.Id);
        entity.Property(e => e.FirstName).HasMaxLength(50).IsRequired();
        entity.Property(e => e.LastName).HasMaxLength(50).IsRequired();
        entity.Property(e => e.PhoneCode).HasMaxLength(10).IsRequired();
        entity.Property(e => e.PhoneNumber).HasMaxLength(20).IsRequired();
        entity.Property(e => e.Language).HasMaxLength(10).IsRequired();
        entity.Property(e => e.CreatedAt).IsRequired();
        entity.Property(e => e.UpdatedAt).IsRequired();

        entity.HasOne(e => e.User)
              .WithOne(u => u.Information)
              .HasForeignKey<UserInformation>(e => e.UserId)
              .OnDelete(DeleteBehavior.Cascade);

        entity.HasIndex(e => e.UserId).IsUnique();
    }
}
