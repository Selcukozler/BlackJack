using BlackJack.Models;
using BlackJack.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// 1. Veritabanı Yapılandırması (SQL Server)
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// 2. Identity (Kullanıcı Yönetimi) Yapılandırması
builder.Services.AddDefaultIdentity<ApplicationUser>(options => {
    options.SignIn.RequireConfirmedAccount = false;
    options.Password.RequireNonAlphanumeric = false;
})
.AddEntityFrameworkStores<ApplicationDbContext>();

// 3. Servis Katmanı Enjeksiyonu (DI)
builder.Services.AddScoped<IBlackjackService, BlackjackService>();

// 4. MVC Controller ve View desteği
builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();

// 5. Session (Oturum) Yapılandırması
builder.Services.AddSession(options => {
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

var app = builder.Build();

// Hata yönetimi ve HTTP yönlendirmeleri
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();

// Statik dosyaların (Bootstrap, CSS, JS) yüklenmesi için GEREKLİ
app.UseStaticFiles(); 

app.UseRouting();

// Session kullanımı (Routing'den sonra, Auth'dan önce olmalı)
app.UseSession();

// Kimlik doğrulama ve Yetkilendirme
app.UseAuthentication();
app.UseAuthorization();

// Uygulama açıldığında direkt Game sayfasına gitmesi için varsayılan rota güncellendi
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Game}/{action=Index}/{id?}");
app.MapRazorPages();
app.Run();