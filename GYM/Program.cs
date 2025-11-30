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

// Registrar servicios personalizados
builder.Services.AddScoped<MembresiaPermisosService>();
builder.Services.AddHostedService<MembresiaExpiradaBackgroundService>();

var app = builder.Build();

// Aplicar migraciones con manejo de errores
try
{
    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<AppDBContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

        try
        {
            db.Database.Migrate();
            logger.LogInformation("Migraciones aplicadas correctamente");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error al aplicar migraciones. La aplicación continuará pero la BD puede no estar actualizada");
            // No lanzar excepción para permitir que la app inicie
        }
    }
}
catch (Exception ex)
{
    var logger = app.Services.GetRequiredService<ILogger<Program>>();
    logger.LogError(ex, "Error crítico durante la inicialización de la base de datos");
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAccesibilidad();

app.UseAuthentication();
app.UseAuthorization();  // ? ANTES del middleware personalizado
app.UseMembresiaExpiradaCheck();  // ? DESPUÉS
app.UseSession();

// Rutas específicas
app.MapControllerRoute(
    name: "gestionrutinas",
    pattern: "GestionRutinas/{action=Index}/{id?}",
    defaults: new { controller = "GestionRutinas" });

app.MapControllerRoute(
    name: "rutinas",
    pattern: "Rutinas/{action=Index}/{id?}",
    defaults: new { controller = "Rutinas" });

// Ruta por defecto (solo una vez)
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Acceso}/{action=Login}/{id?}");

app.Run();