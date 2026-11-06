using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace TaskFlow.Models;

/*bu sınıf IdentityUser’ın tüm özelliklerini miras alır” demektir. IdentityUser ASP.NET Identity’nin hazır kullanıcı sınıfıdır:
Id (string, GUID metni)
UserName
Email
PasswordHash (şifrenin kendisi değil, şifrelenmiş hali)
PhoneNumber gibi alanlar
*/
public class ApplicationUser : IdentityUser
{
    [Required(ErrorMessage = "Ad soyad zorunludur.")]
    [StringLength(100, ErrorMessage = "Ad soyad en fazla 100 karakter olabilir.")]
    [Display(Name = "Ad Soyad")]
    public string FullName { get; set; } = string.Empty;

    // Kullanıcının üyesi olduğu ekipler
    public ICollection<TeamMember> TeamMemberships { get; set; } = new List<TeamMember>();

    // Kullanıcının görev atamaları (her biri: bir görev + kişiye özel deadline/durum)
    public ICollection<TaskAssignment> Assignments { get; set; } = new List<TaskAssignment>();

    // Kullanıcının yazdığı yorumlar
    public ICollection<Comment> Comments { get; set; } = new List<Comment>();
}
