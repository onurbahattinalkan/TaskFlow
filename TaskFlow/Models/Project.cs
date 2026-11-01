/*
public ... {get; set;} = null!;  //null ama sonra doldurulabilir
public ... {get; set;} = string.Empty;  //boş string ile başlatılır
public ... {get; set;} = new List<TeamMember>();  //boş liste ile başlatılır
public ... {get; set;} = DateTime.UtcNow;  //şu anki zamanı alır ve UTC olarak alır
public ... {get; set;} // başlangıç değeri yok, ama derleyzici uyarı verir 
*/

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TaskFlow.Models;

//bizim projemizin Project sınıfını temsil eder. Bu sınıf, projenin temel bilgilerini ve ilişkili varlıkları içerir
public class Project
{
    public int Id { get; set; }

    //Required: zorunlu alan
    //Display: kullanıcıya gösterilecek isim
    [Required(ErrorMessage = "Proje adı zorunludur.")]
    [StringLength(150, ErrorMessage = "Proje adı en fazla 150 karakter olabilir.")]
    [Display(Name = "Proje Adı")]
    public string Name { get; set; } = string.Empty;

    [StringLength(1000, ErrorMessage = "Açıklama en fazla 1000 karakter olabilir.")]
    [Display(Name = "Açıklama")]
    public string? Description { get; set; }

    [Display(Name = "Başlangıç Tarihi")]
    public DateOnly? StartDate { get; set; }

    [Display(Name = "Bitiş Tarihi")]
    public DateOnly? EndDate { get; set; }

    // Her proje bir ekibe aittir (Team 1 - N Project)
    [Required(ErrorMessage = "Ekip seçilmelidir.")]
    [Display(Name = "Ekip")]
    public int TeamId { get; set; }

    [ForeignKey(nameof(TeamId))]
    public Team? Team { get; set; }

    // Projenin görevleri (Project 1 - N TaskItem)
    public ICollection<TaskItem> Tasks { get; set; } = new List<TaskItem>();
}
