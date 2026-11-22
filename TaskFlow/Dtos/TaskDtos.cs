using System.ComponentModel.DataAnnotations;
using TaskFlow.Models;

namespace TaskFlow.Dtos;

// =====================================================================
//  DIŞARI GİDEN VERİ (API cevapları)
// =====================================================================

/// <summary>Görevin kategorisi.</summary>
public class CategoryDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;

    /// <summary>Rozet rengi, #RRGGBB biçiminde.</summary>
    public string Color { get; set; } = string.Empty;
}

/// <summary>Görevin bir kişiye ataması (kişiye özel son tarih ve durum).</summary>
public class AssignmentDto
{
    /// <summary>Atamanın Id'si.</summary>
    public int Id { get; set; }

    public string UserId { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;

    /// <summary>Son teslim zamanı (UTC).</summary>
    public DateTime DueDate { get; set; }

    /// <summary>Todo, InProgress ya da Done.</summary>
    public TaskItemStatus Status { get; set; }

    /// <summary>Bu kişiye atandığı zaman (UTC).</summary>
    public DateTime AssignedAt { get; set; }
}

/// <summary>Görev (herkes için ortak bilgiler + atamaları).</summary>
public class TaskDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int ProjectId { get; set; }

    /// <summary>Oluşturulma zamanı (UTC).</summary>
    public DateTime CreatedAt { get; set; }

    public CategoryDto Category { get; set; } = new();

    /// <summary>
    /// Lider/Admin için tüm atamalar; normal üye için sadece kendi ataması.
    /// </summary>
    public List<AssignmentDto> Assignments { get; set; } = new();
}

// =====================================================================
//  İÇERİ GELEN VERİ (POST / PUT gövdeleri)
// =====================================================================

/// <summary>Görevin atanacağı kişi ve onun son tarihi.</summary>
public class AssigneeDto
{
    /// <summary>Ekip üyesinin kullanıcı Id'si.</summary>
    [Required(ErrorMessage = "Kullanıcı Id'si zorunludur.")]
    public string UserId { get; set; } = string.Empty;

    /// <summary>Son teslim zamanı, saat dilimiyle. Örn: 2026-11-25T18:00:00+03:00</summary>
    // Nullable + Required: gönderilmezse 0001-01-01 olmasın, 400 hatası dönsün
    [Required(ErrorMessage = "Son tarih zorunludur.")]
    public DateTimeOffset? DueDate { get; set; }
}

/// <summary>Görev düzenleme isteği (PUT). Proje değiştirilemez.</summary>
public class TaskUpdateDto
{
    [Required(ErrorMessage = "Görev adı zorunludur.")]
    [StringLength(200, ErrorMessage = "Görev adı en fazla 200 karakter olabilir.")]
    public string Title { get; set; } = string.Empty;

    [StringLength(2000, ErrorMessage = "Açıklama en fazla 2000 karakter olabilir.")]
    public string? Description { get; set; }

    /// <summary>Mevcut bir kategorinin Id'si. NewCategoryName doluysa yok sayılır.</summary>
    public int? CategoryId { get; set; }

    /// <summary>Yeni kategori adı. Doluysa bu isimde kategori kullanılır/oluşturulur.</summary>
    [StringLength(50, ErrorMessage = "Kategori adı en fazla 50 karakter olabilir.")]
    public string? NewCategoryName { get; set; }

    /// <summary>Yeni kategorinin rengi (#RRGGBB, paletten). Boşsa varsayılan renk.</summary>
    public string? NewCategoryColor { get; set; }

    /// <summary>Görevin atanacağı kişiler (en az 1).</summary>
    [Required(ErrorMessage = "Atanan kişi listesi zorunludur.")]
    [MinLength(1, ErrorMessage = "En az bir kişi seçmelisiniz.")]
    public List<AssigneeDto> Assignees { get; set; } = new();
}

/// <summary>Yeni görev isteği (POST). Düzenlemedeki alanlar + proje.</summary>
public class TaskCreateDto : TaskUpdateDto
{
    /// <summary>Görevin ekleneceği projenin Id'si.</summary>
    // int gönderilmezse 0 olur; Range ile 0'ı reddediyoruz
    [Range(1, int.MaxValue, ErrorMessage = "Geçerli bir proje Id'si girin.")]
    public int ProjectId { get; set; }
}