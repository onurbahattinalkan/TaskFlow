using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TaskFlow.Models;

public class Project
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Proje adı zorunludur.")]
    [StringLength(150, ErrorMessage = "Proje adı en fazla 150 karakter olabilir.")]
    [Display(Name = "Proje Adı")]
    public string Name { get; set; } = string.Empty;

    [StringLength(1000, ErrorMessage = "Açıklama en fazla 1000 karakter olabilir.")]
    [Display(Name = "Açıklama")]
    public string? Description { get; set; }

    [Display(Name = "Başlangıç Tarihi")]
    public DateOnly? StartDate { get; set; }

    [Display(Name = "Bitiş Tarihi")]
    public DateOnly? EndDate { get; set; }

    // Her proje bir ekibe aittir (Team 1 - N Project)
    [Required(ErrorMessage = "Ekip seçilmelidir.")]
    [Display(Name = "Ekip")]
    public int TeamId { get; set; }

    [ForeignKey(nameof(TeamId))]
    public Team? Team { get; set; }

    // Projenin görevleri (Project 1 - N TaskItem)
    public ICollection<TaskItem> Tasks { get; set; } = new List<TaskItem>();
}
