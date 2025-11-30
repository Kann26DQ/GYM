using GYM.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace GYM.Middleware
{
    public class MembresiaExpiradaMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<MembresiaExpiradaMiddleware> _logger;

        public MembresiaExpiradaMiddleware(RequestDelegate next, ILogger<MembresiaExpiradaMiddleware> logger)
        {
            _next = next;
            _logger = logger;
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

                            // Si no tiene membresía vigente, desactivar cuenta y LIMPIAR GRUPOS
                            if (!tieneMembresiaVigente)
                            {
                                _logger.LogWarning($"[MIDDLEWARE] Usuario {userId} sin membresía vigente - Desactivando y limpiando grupos");

                                usuario.Activo = false;

                                // Desactivar todas las membresías
                                var membresiasActivas = await dbContext.MembresiasUsuarios
                                    .Where(m => m.UsuarioId == userId && m.Activa)
                                    .ToListAsync();

                                foreach (var membresia in membresiasActivas)
                                {
                                    membresia.Activa = false;
                                }

                                // ? NUEVO: Eliminar evaluaciones (sacar del grupo)
                                var evaluaciones = await dbContext.EvaluacionesRendimiento
                                    .Include(e => e.Rutinas)
                                    .Where(e => e.ClienteId == userId)
                                    .ToListAsync();

                                if (evaluaciones.Any())
                                {
                                    _logger.LogInformation($"[MIDDLEWARE] Eliminando {evaluaciones.Count} evaluaciones del usuario {userId}");

                                    foreach (var evaluacion in evaluaciones)
                                    {
                                        // Eliminar rutinas asociadas
                                        if (evaluacion.Rutinas != null && evaluacion.Rutinas.Any())
                                        {
                                            _logger.LogInformation($"[MIDDLEWARE] Eliminando {evaluacion.Rutinas.Count} rutinas de evaluación {evaluacion.EvaluacionRendimientoId}");

                                            foreach (var rutina in evaluacion.Rutinas)
                                            {
                                                // Eliminar ejercicios de la rutina
                                                var ejercicios = await dbContext.Ejercicios
                                                    .Where(ej => ej.RutinaId == rutina.RutinaId)
                                                    .ToListAsync();

                                                if (ejercicios.Any())
                                                {
                                                    dbContext.Ejercicios.RemoveRange(ejercicios);
                                                }
                                            }

                                            dbContext.Rutinas.RemoveRange(evaluacion.Rutinas);
                                        }

                                        dbContext.EvaluacionesRendimiento.Remove(evaluacion);
                                    }

                                    _logger.LogInformation($"[MIDDLEWARE] Usuario {userId} removido de los grupos del empleado");
                                }

                                // ? NUEVO: Eliminar planes alimenticios
                                var planes = await dbContext.PlanesAlimenticios
                                    .Include(p => p.Comidas)
                                    .Where(p => p.ClienteId == userId)
                                    .ToListAsync();

                                if (planes.Any())
                                {
                                    _logger.LogInformation($"[MIDDLEWARE] Eliminando {planes.Count} planes alimenticios del usuario {userId}");

                                    foreach (var plan in planes)
                                    {
                                        if (plan.Comidas != null && plan.Comidas.Any())
                                        {
                                            dbContext.Comidas.RemoveRange(plan.Comidas);
                                        }
                                        dbContext.PlanesAlimenticios.Remove(plan);
                                    }
                                }

                                await dbContext.SaveChangesAsync();

                                _logger.LogWarning($"[MIDDLEWARE] Usuario {userId} desactivado y limpiado completamente");

                                // Cerrar sesión y redirigir
                                await context.SignOutAsync();
                                context.Response.Redirect("/Acceso/Login?mensaje=membresia-expirada");
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