using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TWL.Server.Persistence.Database.Entities;

[Table("accounts")]
public class AccountEntity
{
    [Key]
    [Column("user_id")]
    public int Id { get; set; }

    [Column("username")]
    [Required]
    [MaxLength(50)]
    public string Username { get; set; } = string.Empty;

    [Column("pass_hash")]
    [Required]
    [MaxLength(128)]
    public string PasswordHash { get; set; } = string.Empty;
}
