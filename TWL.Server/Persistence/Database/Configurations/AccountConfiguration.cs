using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TWL.Server.Persistence.Database.Entities;

namespace TWL.Server.Persistence.Database.Configurations;

public class AccountConfiguration : IEntityTypeConfiguration<AccountEntity>
{
    public void Configure(EntityTypeBuilder<AccountEntity> builder)
    {
        builder.ToTable("accounts");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("user_id").UseIdentityColumn();
        builder.Property(x => x.Username).HasColumnName("username").IsRequired().HasMaxLength(50);
        builder.Property(x => x.PasswordHash).HasColumnName("pass_hash").IsRequired().HasMaxLength(128);

        builder.HasIndex(x => x.Username).IsUnique();
    }
}
