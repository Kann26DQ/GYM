using GYM.Data;
using GYM.Middleware;
using GYM.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.Cookies;
using System;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// Configurar autenticación por cookie (esquema por defecto)
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Acceso/Login";
        options.AccessDeniedPath = "/Acceso/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromHours(1);
        options.SlidingExpiration = true;
    });

builder.Services.AddAuthorization();

builder.Services.AddDbContext<AppDBContext>(options =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("CadenaSQL"));
});

builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// ? Registrar servicios personalizados
builder.Services.AddScoped<MembresiaPermisosService>();
builder.Services.AddHostedService<MembresiaExpiradaBackgroundService>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDBContext>();
    db.Database.Migrate();
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
}
app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAccesibilidad();

app.UseAuthentication();
app.UseMembresiaExpiradaCheck();
app.UseAuthorization();
app.UseSession();

// ? Rutas específicas (ANTES de la ruta por defecto)
// ? Asegurar que las rutas estén correctamente mapeadas
app.MapControllerRoute(
    name: "gestionrutinas",
    pattern: "GestionRutinas/{action=Index}/{id?}",
    defaults: new { controller = "GestionRutinas" });

app.MapControllerRoute(
    name: "rutinas",
    pattern: "Rutinas/{action=Index}/{id?}",
    defaults: new { controller = "Rutinas" });

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Acceso}/{action=Login}/{id?}");

// ? Ruta por defecto (ÚLTIMA)
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Acceso}/{action=Login}/{id?}");

app.Run();