using GYM.Data;
using GYM.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace GYM.Controllers
{
    [Authorize(Roles = "Cliente,Gymbro")]
    public class RutinasController : Controller
    {
        private readonly AppDBContext _context;
        private readonly MembresiaPermisosService _permisosService;

        public RutinasController(AppDBContext context, MembresiaPermisosService permisosService)
        {
            _context = context;
            _permisosService = permisosService;
        }

        public async Task<IActionResult> Index()
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            // Verificar permiso
            var tienePermiso = await _permisosService.TienePermisoRutina(userId);

            if (!tienePermiso)
            {
                TempData["Error"] = "Tu membresía actual no incluye acceso a rutinas. Actualiza tu plan para desbloquear esta función.";
                return RedirectToAction("Index", "MiMembresia");
            }

            // Si tiene permiso, mostrar sus rutinas
            var rutinas = await _context.Rutinas
                .Where(r => r.ClienteId == userId)
                .Include(r => r.Ejercicios)
                .OrderByDescending(r => r.RutinaId) // ?? CORREGIDO: Usar RutinaId en lugar de FechaCreacion
                .ToListAsync();

            return View(rutinas);
        }

        public async Task<IActionResult> Details(int id)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            // Verificar permiso
            var tienePermiso = await _permisosService.TienePermisoRutina(userId);
            if (!tienePermiso)
            {
                TempData["Error"] = "No tienes acceso a esta función con tu membresía actual.";
                return RedirectToAction("Index", "MiMembresia");
            }

            var rutina = await _context.Rutinas
                .Include(r => r.Ejercicios)
                .Include(r => r.Cliente)
                .Include(r => r.Empleado)
                .FirstOrDefaultAsync(r => r.RutinaId == id && r.ClienteId == userId);

            if (rutina == null)
            {
                return NotFound();
            }

            return View(rutina);
        }
    }
}