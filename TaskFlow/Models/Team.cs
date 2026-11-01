using System.ComponentModel.DataAnnotations;

namespace TaskFlow.Models;

//bizim uygulamamızda bir ekip (Team) sınıfını temsil eder. Bu sınıf, ekibin temel bilgilerini ve ilişkili varlıkları içerir.
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
    //ICollection<T>: T tütünde nesneleri tutan liste türü
    public ICollection<TeamMember> Members { get; set; } = new List<TeamMember>();

    // Ekibe ait projeler (Team 1 - N Project)
    public ICollection<Project> Projects { get; set; } = new List<Project>();
}
