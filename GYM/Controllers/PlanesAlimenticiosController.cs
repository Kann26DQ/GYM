using GYM.Data;
using GYM.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace GYM.Controllers
{
    [Authorize(Roles = "Cliente,Gymbro")]
    public class PlanesAlimenticiosController : Controller
    {
        private readonly AppDBContext _context;
        private readonly MembresiaPermisosService _permisosService;

        public PlanesAlimenticiosController(AppDBContext context, MembresiaPermisosService permisosService)
        {
            _context = context;
            _permisosService = permisosService;
        }

        public async Task<IActionResult> Index()
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            // Verificar permiso
            var tienePermiso = await _permisosService.TienePermisoAlimentacion(userId);

            if (!tienePermiso)
            {
                TempData["Error"] = "Tu membresía actual no incluye acceso a planes alimenticios. Actualiza a la Membresía Completa para desbloquear esta función.";
                return RedirectToAction("Index", "MiMembresia");
            }

            // Si tiene permiso, mostrar sus planes
            var planes = await _context.PlanesAlimenticios
                .Where(p => p.ClienteId == userId)
                .Include(p => p.Comidas)
                .OrderByDescending(p => p.PlanAlimenticioId) // ?? CORREGIDO: Usar PlanAlimenticioId en lugar de FechaCreacion
                .ToListAsync();

            return View(planes);
        }

        public async Task<IActionResult> Details(int id)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            // Verificar permiso
            var tienePermiso = await _permisosService.TienePermisoAlimentacion(userId);
            if (!tienePermiso)
            {
                TempData["Error"] = "No tienes acceso a esta función con tu membresía actual.";
                return RedirectToAction("Index", "MiMembresia");
            }

            var plan = await _context.PlanesAlimenticios
                .Include(p => p.Comidas)
                .Include(p => p.Cliente)
                .Include(p => p.Empleado)
                .FirstOrDefaultAsync(p => p.PlanAlimenticioId == id && p.ClienteId == userId);

            if (plan == null)
            {
                return NotFound();
            }

            return View(plan);
        }
    }
}