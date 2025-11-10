using System.Diagnostics;
using System.Security.Claims;
using GYM.Data;
using GYM.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GYM.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly AppDBContext _ctx;

        public HomeController(ILogger<HomeController> logger, AppDBContext ctx)
        {
            _logger = logger;
            _ctx = ctx;
        }

        public async Task<IActionResult> Index(string tipo = "mensual")
        {
            var userRole = User.FindFirstValue(ClaimTypes.Role);
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // ?? NUEVO: Verificar si debe mostrar mensaje de ascenso
            if (!string.IsNullOrEmpty(userId) && int.TryParse(userId, out var userIdInt))
            {
                var usuario = await _ctx.Usuarios.FindAsync(userIdInt);
                if (usuario != null && usuario.MostrarMensajeAscenso && usuario.RolId == 2)
                {
                    ViewData["MostrarMensajeAscenso"] = true;
                    ViewData["NombreUsuario"] = usuario.Nombre;

                    // Marcar como ya mostrado
                    usuario.MostrarMensajeAscenso = false;
                    await _ctx.SaveChangesAsync();
                }
            }

            if (userRole == "Cliente" || userRole == "Gymbro")
            {
                var now = DateTime.UtcNow;
                var tieneMembresia = await _ctx.MembresiasUsuarios
                    .AnyAsync(m => m.UsuarioId == int.Parse(userId!) && m.Activa && m.FechaInicio <= now && m.FechaFin >= now);

                IQueryable<MembresiaPlan> query = _ctx.MembresiaPlanes
                    .AsNoTracking()
                    .Where(p => p.Activo);

                if (tipo == "anual")
                {
                    query = query.Where(p => p.DuracionDias >= 300 && p.DuracionDias <= 365);
                }
                else
                {
                    query = query.Where(p => p.DuracionDias < 100);
                }

                var membresias = await query
                    .OrderBy(p => p.Precio)
                    .Take(4)
                    .ToListAsync();

                ViewData["TieneMembresia"] = tieneMembresia;
                ViewData["Membresias"] = membresias;
                ViewData["TipoMembresia"] = tipo;
            }

            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}