using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TaskFlow.Models;

// TaskItem: görevin HERKES İÇİN ORTAK bilgisi (başlık, açıklama, kategori).
// Kişiye özel bilgiler (deadline, durum) TaskAssignment'ta tutulur.
// Veritabanındaki tablo adı "Tasks".
[Table("Tasks")]
public class TaskItem
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Görev adı zorunludur.")]
    [StringLength(200, ErrorMessage = "Görev adı en fazla 200 karakter olabilir.")]
    [Display(Name = "Görev Adı")]
    public string Title { get; set; } = string.Empty;

    [StringLength(2000, ErrorMessage = "Açıklama en fazla 2000 karakter olabilir.")]
    [Display(Name = "Açıklama")]
    public string? Description { get; set; }

    [Display(Name = "Oluşturulma Tarihi")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Görev bir projeye aittir (Project 1 - N TaskItem)
    [Required(ErrorMessage = "Proje seçilmelidir.")]
    [Display(Name = "Proje")]
    public int ProjectId { get; set; }

    [ForeignKey(nameof(ProjectId))]
    public Project? Project { get; set; }

    // Kategori zorunlu (Category 1 - N TaskItem)
    [Required(ErrorMessage = "Kategori seçilmelidir.")]
    [Display(Name = "Kategori")]
    public int CategoryId { get; set; }

    [ForeignKey(nameof(CategoryId))]
    public Category? Category { get; set; }

    // Görevin atamaları: kime, hangi deadline ile, hangi durumda (TaskItem 1 - N TaskAssignment)
    public ICollection<TaskAssignment> Assignments { get; set; } = new List<TaskAssignment>();

    // Görevin yorumları (TaskItem 1 - N Comment)
    public ICollection<Comment> Comments { get; set; } = new List<Comment>();
}