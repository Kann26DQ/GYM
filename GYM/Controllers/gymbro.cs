using GYM.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace GYM.Controllers
{
    [Authorize(Roles = "Gymbro")]
    public class gymbro : Controller
    {
        private readonly AppDBContext _ctx;
        private readonly ILogger<gymbro> _logger;

        public gymbro(ILogger<gymbro> logger, AppDBContext ctx)
        {
            _logger = logger;
            _ctx = ctx;
        }

        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // Verificar si tiene una membresía activa
            var now = DateTime.UtcNow;
            var tieneMembresia = await _ctx.MembresiasUsuarios
                .AnyAsync(m => m.UsuarioId == int.Parse(userId!) && m.Activa && m.FechaInicio <= now && m.FechaFin >= now);

            // Obtener solo 2 membresías activas
            var membresias = await _ctx.MembresiaPlanes
                .AsNoTracking()
                .Where(p => p.Activo)
                .OrderBy(p => p.Precio)
                .Take(2)
                .ToListAsync();

            ViewData["TieneMembresia"] = tieneMembresia;
            ViewData["Membresias"] = membresias;

            return View("~/Views/Home/Index.cshtml"); // Usa la misma vista que Home
        }
    }
}