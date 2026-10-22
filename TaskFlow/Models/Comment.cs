using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TaskFlow.Models;

public class Comment
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Yorum metni zorunludur.")]
    [StringLength(1000, ErrorMessage = "Yorum en fazla 1000 karakter olabilir.")]
    [Display(Name = "Yorum")]
    public string Text { get; set; } = string.Empty;

    [Display(Name = "Tarih")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Yorumun ait olduğu görev (TaskItem 1 - N Comment)
    public int TaskItemId { get; set; }

    [ForeignKey(nameof(TaskItemId))]
    public TaskItem? TaskItem { get; set; }

    // Yorumu yazan kullanıcı
    [Required]
    public string UserId { get; set; } = string.Empty;

    [ForeignKey(nameof(UserId))]
    public ApplicationUser? User { get; set; }
}
