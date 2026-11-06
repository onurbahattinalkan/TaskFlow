using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using TaskFlow.Helpers;
using TaskFlow.Models;

namespace TaskFlow.ViewModels;

// Formdaki bir kişi satırı: seçildi mi + deadline'ı
public class AssigneeInput
{
    // Formda gizli alan olarak gelir
    public string UserId { get; set; } = string.Empty;

    // Sadece ekranda göstermek için; formdan GELMEZ.
    // [ValidateNever]: "zorunlu alan" kontrolüne girmesin (Nullable açık olduğu için gerekli)
    [ValidateNever]
    public string UserName { get; set; } = string.Empty;

    [ValidateNever]
    public string FullName { get; set; } = string.Empty;

    // Checkbox işaretli mi
    public bool Selected { get; set; }

    // Türkiye saatiyle seçilen deadline (datetime-local kutusu). Kaydederken UTC'ye çevrilir.
    [Display(Name = "Deadline")]
    public DateTime? DueDate { get; set; }
}

// Yeni görev formunun tamamı
public class TaskCreateViewModel
{
    // Hangi projeye ekleniyor (gizli alan)
    public int ProjectId { get; set; }

    [Required(ErrorMessage = "Görev adı zorunludur.")]
    [StringLength(200, ErrorMessage = "Görev adı en fazla 200 karakter olabilir.")]
    [Display(Name = "Görev Adı")]
    public string Title { get; set; } = string.Empty;

    [StringLength(2000, ErrorMessage = "Açıklama en fazla 2000 karakter olabilir.")]
    [Display(Name = "Açıklama")]
    public string? Description { get; set; }

    // Mevcut kategorilerden seçilen (boş olabilir: yeni kategori yazıldıysa)
    [Display(Name = "Kategori")]
    public int? CategoryId { get; set; }

    // Yeni kategori adı (doluysa CategoryId yerine bu kullanılır)
    [StringLength(50, ErrorMessage = "Kategori adı en fazla 50 karakter olabilir.")]
    [Display(Name = "Yeni kategori")]
    public string? NewCategoryName { get; set; }

    // Yeni kategorinin rengi (paletten)
    public string NewCategoryColor { get; set; } = CategoryColors.Default;

    // Ekibin tüm üyeleri; işaretli olanlara atanır
    public List<AssigneeInput> Assignees { get; set; } = new();
}

// Listede tek bir atama satırı
public class TaskListRow
{
    public int TaskId { get; set; }
    public int AssignmentId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public string CategoryColor { get; set; } = CategoryColors.Default;
    public DateTime DueDate { get; set; }      // UTC
    public DateTime AssignedAt { get; set; }   // UTC
    public DateTime CreatedAt { get; set; }    // UTC (eşit deadline'da sıralama için)
    public TaskItemStatus Status { get; set; }
}

// Bir kişinin görevleri (listede bir grup)
public class TaskGroup
{
    public string UserId { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public List<TaskListRow> Rows { get; set; } = new();
}

// Yönetici "Görevler" sayfasının tamamı
public class TaskManageViewModel
{
    public int ProjectId { get; set; }
    public string ProjectName { get; set; } = string.Empty;

    // Üstteki form
    public TaskCreateViewModel Form { get; set; } = new();

    // Formdaki kategori açılır listesi
    public List<Category> Categories { get; set; } = new();

    // Alttaki kişiye göre gruplu liste
    public List<TaskGroup> Groups { get; set; } = new();
}