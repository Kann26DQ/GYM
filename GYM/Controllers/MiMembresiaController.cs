using GYM.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace GYM.Controllers
{
    [Authorize(Roles = "Cliente,Gymbro")]
    public class MiMembresiaController : Controller
    {
        private readonly AppDBContext _ctx;

        public MiMembresiaController(AppDBContext ctx)
        {
            _ctx = ctx;
        }

        public async Task<IActionResult> Index()
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var now = DateTime.UtcNow;

            // Obtener membresía activa actual
            var membresiaActual = await _ctx.MembresiasUsuarios
                .Include(m => m.Plan)
                .Where(m => m.UsuarioId == userId && m.Activa && m.FechaInicio <= now && m.FechaFin >= now)
                .OrderByDescending(m => m.FechaInicio)
                .FirstOrDefaultAsync();

            // Obtener todos los planes disponibles ordenados por precio
            var todosLosPlanes = await _ctx.MembresiaPlanes
                .Where(p => p.Activo)
                .OrderBy(p => p.Precio)
                .ToListAsync();

            // Determinar si tiene el plan más caro
            var planMasCaro = todosLosPlanes.LastOrDefault();
            var tienePlanMasCaro = membresiaActual != null &&
                                   planMasCaro != null &&
                                   membresiaActual.MembresiaPlanId == planMasCaro.MembresiaPlanId;

            // Obtener planes superiores (solo los que son más caros que el actual)
            var planesSuperiores = todosLosPlanes;
            if (membresiaActual != null)
            {
                planesSuperiores = todosLosPlanes
                    .Where(p => p.Precio > membresiaActual.Plan!.Precio)
                    .ToList();
            }

            ViewData["MembresiaActual"] = membresiaActual;
            ViewData["TienePlanMasCaro"] = tienePlanMasCaro;
            ViewData["PlanesSuperiores"] = planesSuperiores;

            return View();
        }
    }
}