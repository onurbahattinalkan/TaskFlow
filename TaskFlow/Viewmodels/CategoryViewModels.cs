using System.ComponentModel.DataAnnotations;
using TaskFlow.Helpers;
namespace TaskFlow.ViewModels;


// Kategori listesindeki tek satır
public class CategoryListItem
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Color { get; set; } = string.Empty;

    // Bu kategoride kaç görev var (0 ise silinebilir)
    public int TaskCount { get; set; }
}

// Kategoriler sayfasının tamamı
public class CategoryIndexViewModel
{
    public int ProjectId { get; set; }
    public string ProjectName { get; set; } = string.Empty;
    public List<CategoryListItem> Items { get; set; } = new();
}

// Ekleme ve düzenleme formu (ikisi aynı alanları kullanır)
public class CategoryFormViewModel
{
    // 0 → yeni kategori, 0'dan büyük → düzenlenen kategorinin Id'si
    public int Id { get; set; }

    // Hangi projeye ait (gizli alan)
    public int ProjectId { get; set; }

    [Required(ErrorMessage = "Kategori adı zorunludur.")]
    [StringLength(50, ErrorMessage = "Kategori adı en fazla 50 karakter olabilir.")]
    [Display(Name = "Kategori Adı")]
    public string Name { get; set; } = string.Empty;

    // Paletten seçilen renk (#RRGGBB)
    [Display(Name = "Renk")]
    public string Color { get; set; } = CategoryColors.Default;
}

// Silme onay sayfası
public class CategoryDeleteViewModel
{
    public int Id { get; set; }
    public int ProjectId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Color { get; set; } = string.Empty;

    // 0'dan büyükse silinemez
    public int TaskCount { get; set; }
}