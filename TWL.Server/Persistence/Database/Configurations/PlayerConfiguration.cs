using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TWL.Server.Persistence.Database.Entities;

namespace TWL.Server.Persistence.Database.Configurations;

public class PlayerConfiguration : IEntityTypeConfiguration<PlayerEntity>
{
    public void Configure(EntityTypeBuilder<PlayerEntity> builder)
    {
        builder.ToTable("players");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("player_id").UseIdentityColumn();

        builder.Property(x => x.AccountId).HasColumnName("user_id");
        builder.Property(x => x.Name).HasColumnName("name").HasMaxLength(50);

        // Stats
        builder.Property(x => x.X).HasColumnName("pos_x");
        builder.Property(x => x.Y).HasColumnName("pos_y");
        builder.Property(x => x.MapId).HasColumnName("map_id");
        builder.Property(x => x.Hp).HasColumnName("hp");

        // JSONB
        builder.Property(x => x.Data).HasColumnName("data").HasColumnType("jsonb");

        // Relationships
        builder.HasOne(x => x.Account)
               .WithMany()
               .HasForeignKey(x => x.AccountId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}
