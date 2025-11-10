using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

namespace GYM.Middleware
{
    /// <summary>
    /// Middleware para gestionar preferencias de accesibilidad del usuario
    /// ODS 10: Reducción de Desigualdades - Software Inclusivo
    /// </summary>
    public class AccesibilidadMiddleware
    {
        private readonly RequestDelegate _next;

        public AccesibilidadMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // Detectar preferencias de accesibilidad del usuario
            var altoContraste = context.Request.Cookies["AltoContraste"] == "true";
            var textoGrande = context.Request.Cookies["TextoGrande"] == "true";
            var reducirAnimaciones = context.Request.Cookies["ReducirAnimaciones"] == "true";

            // Pasar al ViewData para usar en las vistas
            context.Items["AltoContraste"] = altoContraste;
            context.Items["TextoGrande"] = textoGrande;
            context.Items["ReducirAnimaciones"] = reducirAnimaciones;

            await _next(context);
        }
    }

    public static class AccesibilidadMiddlewareExtensions
    {
        public static IApplicationBuilder UseAccesibilidad(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<AccesibilidadMiddleware>();
        }
    }
}