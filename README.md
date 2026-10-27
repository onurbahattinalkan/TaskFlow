# TaskFlow — Proje Yönetim Platformu

BLM4581 Ağ Tabanlı Teknolojiler ve Uygulamaları dersi (2026–2027 Güz) dönem projesi.

TaskFlow, ekiplerin projelerini ve görevlerini tek bir web portalı üzerinden planlayıp takip edebildiği, Trello/Jira benzeri sade bir proje ve görev yönetim sistemidir. Kanban panosu, görev atama ve takip gibi güçlü yanları korur; karmaşıklığı ve gereksiz ayar yükünü azaltır.

## Grup

| Ad Soyad | Öğrenci No | GitHub |
| --- | --- | --- |
| Onur Bahattin ALKAN | 22290551 | [@onurbahattinalkan](https://github.com/onurbahattinalkan) |
| Zeynep AKÇAY | 23291339 | [@zeynepakcayy](https://github.com/zeynepakcayy) |

## Özellikler

Roller: **Yönetici (Admin)** ve **Kullanıcı (User)** (ASP.NET Identity).

- [ ] Üyelik, giriş, çıkış ve rol tabanlı yetkilendirme
- [ ] Ekip oluşturma, ekibe üye ekleme (yalnızca ekip üyeleri ilgili projeleri görür)
- [ ] Proje ekleme, listeleme, düzenleme, silme
- [ ] Görev oluşturma, kişiye atama, durum ve son tarih belirleme
- [ ] Sürükle-bırak kanban panosu (SortableJS)
- [ ] Görevlere yorum ekleme
- [ ] Duruma göre filtreleme ve arama
- [ ] Uygulama içi bildirimler (görev atandı, yorum geldi)
- [ ] Geciken işler ve kişi başı iş yükü raporu
- [ ] Yönetici paneli (kullanıcı, rol, ekip ve proje yönetimi)
- [ ] Görevler için tam CRUD Web API + Swagger
- [ ] Mobil uyumlu arayüz

> Tamamlanan özellikler geliştikçe işaretlenecektir.

## Teknolojiler

- ASP.NET Core **MVC**
- Entity Framework Core + migration
- **PostgreSQL**
- ASP.NET Identity
- Bootstrap (mobil uyumlu arayüz)
- SortableJS
- Swagger (Swashbuckle)
- DataAnnotations ile model doğrulama

## Veritabanı Tabloları

| Tablo | İçerik | İlişkiler |
| --- | --- | --- |
| Users | Identity kullanıcıları (roller Identity rol tablolarında) | TeamMembers, Tasks, Comments |
| Teams | Ekip adı, açıklama, oluşturulma tarihi | TeamMembers (1-N), Projects (1-N) |
| TeamMembers | Kullanıcı–ekip bağlantısı, ekip içi rol | Users, Teams |
| Projects | Proje adı, açıklama, tarihler; bir ekibe aittir | Teams, Tasks (1-N) |
| Tasks | Başlık, açıklama, durum, atanan kişi, son tarih | Projects, Users, Comments (1-N) |
| Comments | Görev yorumları: metin, yazan kullanıcı, tarih | Tasks, Users |

## Kurulum ve Çalıştırma

### Gereksinimler

- [.NET SDK](https://dotnet.microsoft.com/download) (TaskFlow: sürüm)
- [PostgreSQL](https://www.postgresql.org/download/) (TaskFlow: sürüm)
- `dotnet-ef` aracı:

```bash
dotnet tool install --global dotnet-ef
```

### 1. Depoyu klonlayın

```bash
git clone https://github.com/onurbahattinalkan/TaskFlow.git
cd TaskFlow
```

### 2. Veritabanını hazırlayın

PostgreSQL'de boş bir veritabanı oluşturun:

```sql
CREATE DATABASE taskflow;
```

### 3. Bağlantı dizesini user-secrets ile tanımlayın

Gizli bilgiler (veritabanı şifresi, bağlantı dizesi) depoya konmaz; `user-secrets` kullanılır.

```bash
cd TaskFlow
dotnet user-secrets init
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Port=5432;Database=taskflow;Username=KULLANICI;Password=SIFRE"
```

> `KULLANICI` ve `SIFRE` değerlerini kendi PostgreSQL bilgilerinizle değiştirin.

### 4. Migration'ları uygulayın

```bash
dotnet ef database update
```

Bu komut tüm tabloları oluşturur. (Yeni migration eklemek için: `dotnet ef migrations add MigrationAdi`)

### 5. Uygulamayı çalıştırın

```bash
dotnet run
```

Uygulama varsayılan olarak `https://localhost:TaskFlow-PORT` adresinde açılır.

### Varsayılan hesaplar

TODO: Örnek veri (seed) eklendikten sonra yönetici ve test kullanıcısı bilgileri burada yazılacak.

## Web API ve Swagger

Görev (Task) varlığı için tam CRUD uç noktaları bulunur. Swagger arayüzü: `https://localhost:TaskFlow-PORT/swagger`

| Metot | Uç nokta | Açıklama |
| --- | --- | --- |
| GET | `/api/v1/tasks` | Görevleri listeler |
| GET | `/api/v1/tasks/{id}` | Tek görevi getirir |
| POST | `/api/v1/tasks` | Yeni görev ekler |
| PUT | `/api/v1/tasks/{id}` | Görevi günceller |
| DELETE | `/api/v1/tasks/{id}` | Görevi siler |

## Klasör Yapısı

TaskFlow: Proje oluşturulduktan sonra güncellenecek.

```
Controllers/   # MVC ve API controller'ları
Models/        # Entity sınıfları
ViewModels/    # Form ve görünüm modelleri
Data/          # DbContext ve migration'lar
Views/         # Razor görünümleri
wwwroot/       # Statik dosyalar (CSS, JS)
```

## Geliştirme Planı

9 haftalık plan (21 Ekim – 23 Aralık 2026):

| Hafta | Konu |
| --- | --- |
| 1 | Proje iskeleti, PostgreSQL bağlantısı, entity'ler, DbContext, ilk migration |
| 2 | Identity (kayıt/giriş/çıkış, roller), proje CRUD |
| 3 | Görev CRUD ve ilgili sayfalar |
| 4 | Görev Web API + Swagger, yönetici paneli ana sayfası |
| 5 | Yorumlar, görev durumu değiştirme |
| 6 | Filtreleme ve arama, rol yönetimi |
| 7 | Kullanıcıya özel proje görünümü, yönetici proje yönetimi |
| 8 | Hata düzeltmeleri, örnek veri, mobil kontrol |
| 9 | README tamamlama, son testler |

## Lisans

Bu proje, ders kapsamında geliştirilmiş bir dönem projesidir.
