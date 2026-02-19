using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TWL.Server.Persistence.Database.Entities;

namespace TWL.Server.Persistence.Database.Configurations;

public class PlayerConfiguration : IEntityTypeConfiguration<PlayerEntity>
{
    public void Configure(EntityTypeBuilder<PlayerEntity> builder)
    {
        builder.ToTable("players");

        // Primary Key
        builder.HasKey(x => x.PlayerId);
        builder.Property(x => x.PlayerId)
            .HasColumnName("player_id")
            .UseIdentityByDefaultColumn();

        // Foreign Key to accounts
        builder.Property(x => x.UserId).HasColumnName("user_id");
        builder.HasIndex(x => x.UserId).IsUnique();
        builder.HasOne(x => x.Account)
            .WithOne()
            .HasForeignKey<PlayerEntity>(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // Identity
        builder.Property(x => x.Name)
            .HasColumnName("name")
            .HasMaxLength(50)
            .IsRequired();

        // Core Stats
        builder.Property(x => x.Hp).HasColumnName("hp");
        builder.Property(x => x.Sp).HasColumnName("sp");
        builder.Property(x => x.Level).HasColumnName("level");
        builder.Property(x => x.RebirthLevel).HasColumnName("rebirth_level");
        builder.Property(x => x.Exp).HasColumnName("exp");
        builder.Property(x => x.ExpToNextLevel).HasColumnName("exp_to_next_level");
        builder.Property(x => x.StatPoints).HasColumnName("stat_points");
        builder.Property(x => x.Str).HasColumnName("str");
        builder.Property(x => x.Con).HasColumnName("con");
        builder.Property(x => x.Int).HasColumnName("int");
        builder.Property(x => x.Wis).HasColumnName("wis");
        builder.Property(x => x.Agi).HasColumnName("agi");

        // Element (stored as int)
        builder.Property(x => x.Element).HasColumnName("element");

        // Currency
        builder.Property(x => x.Gold).HasColumnName("gold");
        builder.Property(x => x.PremiumCurrency).HasColumnName("premium_currency");
        builder.Property(x => x.DailyGiftAccumulator).HasColumnName("daily_gift_accumulator");
        builder.Property(x => x.LastGiftResetDate).HasColumnName("last_gift_reset_date");

        // Location
        builder.Property(x => x.MapId).HasColumnName("map_id");
        builder.Property(x => x.X).HasColumnName("x");
        builder.Property(x => x.Y).HasColumnName("y");

        // Active Pet
        builder.Property(x => x.ActivePetInstanceId).HasColumnName("active_pet_instance_id");

        // JSONB Columns — arrays default to "[]", objects default to "{}"
        builder.Property(x => x.InventoryJson).HasColumnName("inventory_json").HasColumnType("jsonb").HasDefaultValue("[]");
        builder.Property(x => x.EquipmentJson).HasColumnName("equipment_json").HasColumnType("jsonb").HasDefaultValue("[]");
        builder.Property(x => x.BankJson).HasColumnName("bank_json").HasColumnType("jsonb").HasDefaultValue("[]");
        builder.Property(x => x.PetsJson).HasColumnName("pets_json").HasColumnType("jsonb").HasDefaultValue("[]");
        builder.Property(x => x.SkillsJson).HasColumnName("skills_json").HasColumnType("jsonb").HasDefaultValue("[]");
        builder.Property(x => x.WorldFlagsJson).HasColumnName("world_flags_json").HasColumnType("jsonb").HasDefaultValue("[]");
        builder.Property(x => x.ProcessedOrdersJson).HasColumnName("processed_orders_json").HasColumnType("jsonb").HasDefaultValue("[]");
        builder.Property(x => x.InstanceLockoutsJson).HasColumnName("instance_lockouts_json").HasColumnType("jsonb").HasDefaultValue("{}");
        builder.Property(x => x.QuestStatesJson).HasColumnName("quest_states_json").HasColumnType("jsonb").HasDefaultValue("{}");
        builder.Property(x => x.QuestProgressJson).HasColumnName("quest_progress_json").HasColumnType("jsonb").HasDefaultValue("{}");
        builder.Property(x => x.QuestFlagsJson).HasColumnName("quest_flags_json").HasColumnType("jsonb").HasDefaultValue("[]");
        builder.Property(x => x.QuestCompletionTimesJson).HasColumnName("quest_completion_times_json").HasColumnType("jsonb").HasDefaultValue("{}");
        builder.Property(x => x.QuestStartTimesJson).HasColumnName("quest_start_times_json").HasColumnType("jsonb").HasDefaultValue("{}");

        // Metadata
        builder.Property(x => x.LastSaved).HasColumnName("last_saved");
    }
}
