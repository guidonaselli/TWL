using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TWL.Server.Persistence.Database.Entities;

[Table("players")]
public class PlayerEntity
{
    [Key]
    [Column("player_id")]
    public int Id { get; set; }

    [Column("user_id")]
    public int AccountId { get; set; }

    [Column("name")]
    [MaxLength(50)]
    public string Name { get; set; } = string.Empty;

    [Column("pos_x")]
    public float X { get; set; }

    [Column("pos_y")]
    public float Y { get; set; }

    [Column("map_id")]
    public int MapId { get; set; }

    [Column("hp")]
    public int Hp { get; set; }

    [Column("data", TypeName = "jsonb")]
    public string Data { get; set; } = "{}";

    [ForeignKey("AccountId")]
    public virtual AccountEntity? Account { get; set; }
}
