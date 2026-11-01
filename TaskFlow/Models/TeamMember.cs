using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TaskFlow.Models;

// Kullanıcı - Ekip bağlantı tablosu (N-N ilişkiyi iki 1-N olarak tutar)
public class TeamMember
{
    public int Id { get; set; }

    //db'de tutulan gerçek bilgi
    public int TeamId { get; set; }

    //ForeignKey, veritabanı kavramı “başka bir tablonun satırını gösteren sütun”
    //aşağıdaki Team özelliği yukarıdaki TeamId ile bağlantılı
    [ForeignKey(nameof(TeamId))]

    //kodda TeamId numarasına karşılık gelen ekibin tamamına ulaşmak için bir kısayol
    public Team Team { get; set; } = null!;

    [Required]
    //UserId string çünkü ASP.NET default string kullanıyor
    public string UserId { get; set; } = string.Empty;

    [ForeignKey(nameof(UserId))]
    public ApplicationUser User { get; set; } = null!;  //null olabilir

    // Ekip içi rol (örn. "Lider", "Üye")
    [Required]
    [StringLength(50)]
    [Display(Name = "Ekip İçi Rol")]
    public string TeamRole { get; set; } = "Üye";

    [Display(Name = "Katılma Tarihi")]
    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
}
