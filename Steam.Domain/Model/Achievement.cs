using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Steam.Domain.Model;

public class Achievement : BaseEntity
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Column("id")]
    public Guid Id { get; set; }
    [Column("name")]
    public string Name { get; set; } = string.Empty;
    [Column("description")]
    public string Description { get; set; } = string.Empty;
    [Column("icon_id")]
    public int? IconId { get; set; }
    [ForeignKey(nameof(IconId))]
    public Media? Icon { get; set; }
    [Column("game_id")]
    public Guid GameId { get; set; }
    [ForeignKey("GameId")]
    public Game Game { get; set; } = null!;

    public ICollection<UserAchievement> UserAchievements { get; set; } = [];

}
