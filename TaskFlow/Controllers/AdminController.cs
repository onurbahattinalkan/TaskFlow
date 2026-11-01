using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TaskFlow.Models;
using TaskFlow.ViewModels;

namespace TaskFlow.Controllers;

// Sınıfın başındaki [Authorize]: bu controller'daki TÜM metotlar sadece Admin rolüne açık.
//  - Giriş yapmamış biri  → Login sayfasına
//  - Giriş yapmış ama Admin değil → AccessDenied sayfasına (2.7)
// Yeni metot eklendiğinde ayrıca korumaya gerek yok, otomatik korunur.
[Authorize(Roles = AppRoles.Admin)]
public class AdminController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;

    public AdminController(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    // ------------------------------------------------------------------
    // GET /Admin/Users  → tüm kullanıcıları rolleriyle listele
    // ------------------------------------------------------------------
    [HttpGet]
    public async Task<IActionResult> Users()
    {
        // 1) Tüm kullanıcıları kullanıcı adına göre sıralı al
        var users = await _userManager.Users
            .OrderBy(u => u.UserName)
            .ToListAsync();

        // 2) Her kullanıcının rollerini ekleyerek ViewModel listesine çevir.
        //    Not: Her kullanıcı için ayrı bir rol sorgusu atılıyor (N+1 sorgu).
        //    Ders projesindeki birkaç düzine kullanıcı için sorun değil;
        //    binlerce kullanıcı olsaydı tek bir JOIN sorgusuyla yapardık.
        var model = new List<UserListItemViewModel>();
        foreach (var user in users)
        {
            model.Add(new UserListItemViewModel
            {
                Id = user.Id,
                UserName = user.UserName ?? string.Empty,
                FullName = user.FullName,
                Email = user.Email ?? string.Empty,
                EmailConfirmed = user.EmailConfirmed,
                Roles = await _userManager.GetRolesAsync(user)
            });
        }

        return View(model);
    }
}