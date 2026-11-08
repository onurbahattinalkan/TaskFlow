using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace TaskFlow.Models;

// "Bu görev bu kişiye verildi" satırı.
// Aynı görev 5 kişiye verilirse 5 satır olur; her birinin deadline'ı ve durumu ayrıdır.
// [Index]: aynı görev aynı kişiye iki kez atanamaz
[Index(nameof(TaskItemId), nameof(UserId), IsUnique = true)]
public class TaskAssignment
{
    public int Id { get; set; }

    // Hangi görev
    public int TaskItemId { get; set; }

    [ForeignKey(nameof(TaskItemId))]
    public TaskItem? TaskItem { get; set; }

    // Kime atandı
    [Required]
    public string UserId { get; set; } = string.Empty;

    [ForeignKey(nameof(UserId))]
    public ApplicationUser? User { get; set; }

    // Son teslim zamanı (tarih + saat). Veritabanında her zaman UTC saklanır,
    // ekranda Türkiye saatine çevrilerek gösterilir.
    [Display(Name = "Son Tarih")]
    public DateTime DueDate { get; set; }

    // Kanban sütunu: Yapılacak (Atanan) / Devam Ediyor / Tamamlandı
    [Display(Name = "Durum")]
    public TaskItemStatus Status { get; set; } = TaskItemStatus.Todo;

    // Bu kişiye ne zaman atandı (sıralamada kullanılacak)
    [Display(Name = "Atanma Tarihi")]
    public DateTime AssignedAt { get; set; } = DateTime.UtcNow;
}