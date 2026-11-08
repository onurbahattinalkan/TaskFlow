using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace TaskFlow.Models;

// Görev kategorisi (Hata, Tasarım, Test...). Her proje kendi kategorilerini tutar.
// [Index]: aynı projede aynı isimde iki kategori olamaz (veritabanı seviyesinde benzersizlik)
[Index(nameof(ProjectId), nameof(Name), IsUnique = true)]
public class Category
{
	public int Id { get; set; }

	[Required(ErrorMessage = "Kategori adı zorunludur.")]
	[StringLength(50, ErrorMessage = "Kategori adı en fazla 50 karakter olabilir.")]
	[Display(Name = "Kategori")]
	public string Name { get; set; } = string.Empty;

	// Rozet rengi, "#RRGGBB" biçiminde (ör. #dc3545). 7 karakter yeterli.
	[Required]
	[StringLength(7)]
	[RegularExpression("^#[0-9A-Fa-f]{6}$", ErrorMessage = "Renk #RRGGBB biçiminde olmalıdır.")]
	[Display(Name = "Renk")]
	public string Color { get; set; } = "#6c757d"; // varsayılan: gri

	// Kategori hangi projeye ait (Project 1 - N Category)
	public int ProjectId { get; set; }

	[ForeignKey(nameof(ProjectId))]
	public Project? Project { get; set; }

	// Bu kategorideki görevler
	public ICollection<TaskItem> Tasks { get; set; } = new List<TaskItem>();
}