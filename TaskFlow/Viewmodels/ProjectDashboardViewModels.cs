using TaskFlow.Models;

namespace TaskFlow.ViewModels;

// Sol menüdeki sekmelerin adları (yazım hatasına karşı sabit)
public static class ProjectTabs
{
    public const string Overview = "overview";   // Genel Bakış
    public const string Tasks = "tasks";         // Görevler (yönetici listesi)
    public const string MyTasks = "mytasks";     // Görevlerim (Kanban)
}

// Sol menünün ihtiyaç duyduğu bilgiler. Controller ViewData["ProjectNav"] ile gönderir.
public class ProjectNavViewModel
{
    public int ProjectId { get; set; }
    public string ProjectName { get; set; } = string.Empty;
    public string TeamName { get; set; } = string.Empty;

    // Lider ya da Admin mi? (Görevler sekmesi sadece onlara görünür)
    public bool IsManager { get; set; }

    // Şu an hangi sekme seçili (ProjectTabs sabitlerinden biri)
    public string ActiveTab { get; set; } = ProjectTabs.Overview;
}

// Bir grup atamanın durum istatistikleri (proje geneli, tek kişi vb.)
public class StatusStats
{
    public int Total { get; set; }
    public int Todo { get; set; }        // başlanmamış (Atanan sütunu)
    public int InProgress { get; set; }  // devam eden
    public int Done { get; set; }        // biten
    public int Overdue { get; set; }     // bitmemiş ve deadline'ı geçmiş

    // Toplam 0 ise yüzde de 0 (sıfıra bölme olmasın)
    private int Percent(int count) =>
        Total == 0 ? 0 : (int)Math.Round(100.0 * count / Total);

    public int TodoPercent => Percent(Todo);
    public int InProgressPercent => Percent(InProgress);
    public int DonePercent => Percent(Done);

    // (Durum, Deadline) listesinden istatistik üretir
    public static StatusStats From(IEnumerable<(TaskItemStatus Status, DateTime DueDate)> items)
    {
        var list = items.ToList();
        var now = DateTime.UtcNow;

        return new StatusStats
        {
            Total = list.Count,
            Todo = list.Count(i => i.Status == TaskItemStatus.Todo),
            InProgress = list.Count(i => i.Status == TaskItemStatus.InProgress),
            Done = list.Count(i => i.Status == TaskItemStatus.Done),
            Overdue = list.Count(i => i.Status != TaskItemStatus.Done && i.DueDate < now)
        };
    }
}

// Yönetici tablosunda bir ekip üyesinin satırı
public class MemberProgressRow
{
    public string UserName { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public StatusStats Stats { get; set; } = new();
}

// "Yaklaşan deadline'larım" listesindeki bir satır
public class UpcomingItem
{
    public string Title { get; set; } = string.Empty;
    public DateTime DueDate { get; set; }    // UTC
    public TaskItemStatus Status { get; set; }
}

// Genel Bakış sayfasının tamamı
public class ProjectDashboardViewModel
{
    public int ProjectId { get; set; }
    public string ProjectName { get; set; } = string.Empty;
    public string TeamName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateOnly? StartDate { get; set; }
    public DateOnly? EndDate { get; set; }

    public bool IsManager { get; set; }
    public int TaskCount { get; set; }       // görev sayısı (bir görev birden çok kişiye atanabilir)
    public int MemberCount { get; set; }     // ekip üye sayısı

    public StatusStats ProjectStats { get; set; } = new();   // tüm atamalar
    public StatusStats MyStats { get; set; } = new();        // sadece giriş yapan kişinin atamaları

    public List<UpcomingItem> MyUpcoming { get; set; } = new();     // bitmemiş, en yakın deadline'lar
    public List<MemberProgressRow> Members { get; set; } = new();   // sadece yöneticiye dolu gelir
}

// Yuvarlak yüzde halkasının verisi (_ProgressRing partial'ı kullanır)
// Color: CSS rengi (ör. "#198754"), Caption: halkanın altındaki küçük yazı (ör. "3 / 10")
public record ProgressRingViewModel(int Percent, string Label, string Color, string? Caption = null);