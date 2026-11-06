// kod içinde kullanılacak olan sınıfları ve kütüphaneleri içe aktarır
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using TaskFlow.Models;

namespace TaskFlow.Data;

public class AppDbContext : IdentityDbContext<ApplicationUser>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    // her biri bir tabloyu temsil eder ve her biri için veritabanında bir tablo oluşturulur
    public DbSet<Team> Teams => Set<Team>();
    public DbSet<TeamMember> TeamMembers => Set<TeamMember>();
    public DbSet<Project> Projects => Set<Project>();
    public DbSet<TaskItem> Tasks => Set<TaskItem>();
    public DbSet<Comment> Comments => Set<Comment>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<TaskAssignment> TaskAssignments => Set<TaskAssignment>();

    // Tablolar arası ilişkilerin özel kuralları burada yazılır
    protected override void OnModelCreating(ModelBuilder builder)
    {
        // ÖNEMLİ: Identity tablolarının (AspNetUsers, AspNetRoles...) ayarları
        // base sınıfta yapılıyor; bunu çağırmazsak Identity bozulur.
        base.OnModelCreating(builder);

        // Görevi olan bir kategori silinemesin.
        // NoAction (PostgreSQL'de "NO ACTION"): kontrol ifadenin SONUNDA yapılır.
        //  - Tek başına kategori silinirse ve görevi varsa → hata (istediğimiz)
        //  - Proje silinirse görevler + kategoriler birlikte silinir → sorun yok
        // (Restrict seçseydik proje silme de patlardı.)
        builder.Entity<TaskItem>()
            .HasOne(t => t.Category)
            .WithMany(c => c.Tasks)
            .HasForeignKey(t => t.CategoryId)
            .OnDelete(DeleteBehavior.NoAction);

        // Görev silinince atamaları da silinsin (Cascade)
        builder.Entity<TaskAssignment>()
            .HasOne(a => a.TaskItem)
            .WithMany(t => t.Assignments)
            .HasForeignKey(a => a.TaskItemId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}