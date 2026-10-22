using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace TaskFlow.Models;

// Users tablosu: Identity'nin AspNetUsers tablosu bu sınıfa karşılık gelir.
public class ApplicationUser : IdentityUser
{
    [Required(ErrorMessage = "Ad soyad zorunludur.")]
    [StringLength(100, ErrorMessage = "Ad soyad en fazla 100 karakter olabilir.")]
    [Display(Name = "Ad Soyad")]
    public string FullName { get; set; } = string.Empty;

    // Kullanıcının üyesi olduğu ekipler
    public ICollection<TeamMember> TeamMemberships { get; set; } = new List<TeamMember>();

    // Kullanıcıya atanan görevler
    public ICollection<TaskItem> AssignedTasks { get; set; } = new List<TaskItem>();

    // Kullanıcının yazdığı yorumlar
    public ICollection<Comment> Comments { get; set; } = new List<Comment>();
}
