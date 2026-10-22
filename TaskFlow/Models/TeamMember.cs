using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TaskFlow.Models;

// Kullanıcı - Ekip bağlantı tablosu (çoka-çok ilişkiyi iki bire-çok olarak tutar)
public class TeamMember
{
    public int Id { get; set; }

    public int TeamId { get; set; }

    [ForeignKey(nameof(TeamId))]
    public Team Team { get; set; } = null!;

    [Required]
    public string UserId { get; set; } = string.Empty;

    [ForeignKey(nameof(UserId))]
    public ApplicationUser User { get; set; } = null!;

    // Ekip içi rol (örn. "Lider", "Üye")
    [Required]
    [StringLength(50)]
    [Display(Name = "Ekip İçi Rol")]
    public string TeamRole { get; set; } = "Üye";

    [Display(Name = "Katılma Tarihi")]
    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
}
