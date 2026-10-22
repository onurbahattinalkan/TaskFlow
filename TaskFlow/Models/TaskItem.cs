using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TaskFlow.Models;

// Sınıf adı System.Threading.Tasks.Task ile çakışmasın diye TaskItem.
// Veritabanındaki tablo adı "Tasks" olur.
[Table("Tasks")]
public class TaskItem
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Görev başlığı zorunludur.")]
    [StringLength(200, ErrorMessage = "Başlık en fazla 200 karakter olabilir.")]
    [Display(Name = "Başlık")]
    public string Title { get; set; } = string.Empty;

    [StringLength(2000, ErrorMessage = "Açıklama en fazla 2000 karakter olabilir.")]
    [Display(Name = "Açıklama")]
    public string? Description { get; set; }

    [Display(Name = "Durum")]
    public TaskItemStatus Status { get; set; } = TaskItemStatus.Todo;

    [Display(Name = "Son Tarih")]
    public DateOnly? DueDate { get; set; }

    [Display(Name = "Oluşturulma Tarihi")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Görev bir projeye aittir (Project 1 - N TaskItem)
    [Required(ErrorMessage = "Proje seçilmelidir.")]
    [Display(Name = "Proje")]
    public int ProjectId { get; set; }

    [ForeignKey(nameof(ProjectId))]
    public Project? Project { get; set; }

    // Atanan kişi (boş olabilir: henüz kimseye atanmamış görev)
    [Display(Name = "Atanan Kişi")]
    public string? AssignedUserId { get; set; }

    [ForeignKey(nameof(AssignedUserId))]
    public ApplicationUser? AssignedUser { get; set; }

    // Görevin yorumları (TaskItem 1 - N Comment)
    public ICollection<Comment> Comments { get; set; } = new List<Comment>();
}
