using GYM.Data;
using Microsoft.AspNetCore.Authentication; // ?? AGREGAR ESTA LÍNEA
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace GYM.Middleware
{
    public class MembresiaExpiradaMiddleware
    {
        private readonly RequestDelegate _next;

        public MembresiaExpiradaMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context, AppDBContext dbContext)
        {
            // Solo verificar para usuarios autenticados que NO sean SuperAdmin
            if (context.User.Identity?.IsAuthenticated == true)
            {
                var userRole = context.User.FindFirst(ClaimTypes.Role)?.Value;

                // Solo verificar para Cliente y Gymbro
                if (userRole == "Cliente" || userRole == "Gymbro")
                {
                    var userIdClaim = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                    if (int.TryParse(userIdClaim, out var userId))
                    {
                        var usuario = await dbContext.Usuarios.FindAsync(userId);

                        if (usuario != null && usuario.Activo)
                        {
                            var now = DateTime.UtcNow;

                            // Verificar si tiene membresía activa vigente
                            var tieneMembresiaVigente = await dbContext.MembresiasUsuarios
                                .AnyAsync(m => m.UsuarioId == userId &&
                                              m.Activa &&
                                              m.FechaInicio <= now &&
                                              m.FechaFin >= now);

                            // Si no tiene membresía vigente, desactivar cuenta
                            if (!tieneMembresiaVigente)
                            {
                                usuario.Activo = false;

                                // Desactivar todas las membresías
                                var membresiasActivas = await dbContext.MembresiasUsuarios
                                    .Where(m => m.UsuarioId == userId && m.Activa)
                                    .ToListAsync();

                                foreach (var membresia in membresiasActivas)
                                {
                                    membresia.Activa = false;
                                }

                                await dbContext.SaveChangesAsync();

                                // Cerrar sesión y redirigir
                                await context.SignOutAsync(); // ? Ahora funcionará correctamente
                                context.Response.Redirect("/Acceso/Login");
                                return;
                            }
                        }
                    }
                }
            }

            await _next(context);
        }
    }

    // Extension method para registrar el middleware
    public static class MembresiaExpiradaMiddlewareExtensions
    {
        public static IApplicationBuilder UseMembresiaExpiradaCheck(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<MembresiaExpiradaMiddleware>();
        }
    }
}