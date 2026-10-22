using System.ComponentModel.DataAnnotations;

namespace TaskFlow.Models;

public class Team
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Ekip adı zorunludur.")]
    [StringLength(100, ErrorMessage = "Ekip adı en fazla 100 karakter olabilir.")]
    [Display(Name = "Ekip Adı")]
    public string Name { get; set; } = string.Empty;

    [StringLength(500, ErrorMessage = "Açıklama en fazla 500 karakter olabilir.")]
    [Display(Name = "Açıklama")]
    public string? Description { get; set; }

    [Display(Name = "Oluşturulma Tarihi")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Ekibin üyeleri (Team 1 - N TeamMember)
    public ICollection<TeamMember> Members { get; set; } = new List<TeamMember>();

    // Ekibe ait projeler (Team 1 - N Project)
    public ICollection<Project> Projects { get; set; } = new List<Project>();
}
