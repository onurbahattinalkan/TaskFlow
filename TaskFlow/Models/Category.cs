
/*
NOT:
görevlerle bağlantı (Tasks listesi) şimdilik yok
görev sayfalarını bitirince eklenicek
*/

using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace TaskFlow.Models;

// görevleri gruplamak için kategori (örn. Hata, Yeni Özellik, Tasarım)
// kategori adı benzersiz
[Index(nameof(Name), IsUnique = true)]
public class Category
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Kategori adı zorunludur.")]
    [StringLength(50, ErrorMessage = "Kategori adı en fazla 50 karakter olabilir.")]
    [Display(Name = "Kategori Adı")]
    public string Name { get; set; } = string.Empty;

    [StringLength(200, ErrorMessage = "Açıklama en fazla 200 karakter olabilir.")]
    [Display(Name = "Açıklama")]
    public string? Description { get; set; }
}