using Microsoft.AspNetCore.Mvc.Rendering;
using TaskFlow.Models;

namespace TaskFlow.ViewModels;

// Projeler sayfasındaki TEK bir kartın verisi.
// Veritabanından sadece kartta gösterilecek alanları çekiyoruz (tüm görevleri yüklemeden).
public class ProjectCardViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int TeamId { get; set; }
    public string TeamName { get; set; } = string.Empty;
    public DateOnly? StartDate { get; set; }
    public DateOnly? EndDate { get; set; }

    // Projedeki görev sayısı
    public int TaskCount { get; set; }

    // Toplam atama sayısı ve bunlardan "Tamamlandı" olanlar
    public int AssignmentCount { get; set; }
    public int DoneCount { get; set; }

    // Bu kullanıcı projeyi düzenleyip silebilir mi? (ekip lideri ya da Admin)
    public bool CanManage { get; set; }

    // Tamamlanma yüzdesi: atama yoksa 0, varsa biten / toplam
    public int CompletionPercent =>
        AssignmentCount == 0 ? 0 : (int)Math.Round(100.0 * DoneCount / AssignmentCount);
}

// Projeler sayfasının tamamı: üstte kartlar, altta yeni proje formu
public class ProjectsIndexViewModel
{
    public List<ProjectCardViewModel> Projects { get; set; } = new();

    // Alttaki formun bağlandığı boş (ya da hatalı gönderilmiş) proje
    public Project NewProject { get; set; } = new();

    // Formdaki "Ekip" açılır listesi: sadece lideri olunan ekipler
    public SelectList LeaderTeams { get; set; } = new SelectList(Array.Empty<Team>());

    // Hiçbir ekibin lideri değilse form gösterilmez
    public bool CanCreate => LeaderTeams.Any();
}