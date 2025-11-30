using GYM.Data;
using GYM.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GYM.Controllers
{
    [Authorize(Roles = "Cliente,Gymbro")]
    public class MisComprasController : Controller
    {
        private readonly AppDBContext _context;

        public MisComprasController(AppDBContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var userId = GetCurrentUserId();

            // ✅ Ahora incluye Detalles y Producto para mostrar los nombres
            var ventas = await _context.Ventas
                .AsNoTracking()
                .Include(v => v.Detalles)
                    .ThenInclude(d => d.Producto)
                .Where(v => v.ClienteId == userId || (v.EmpleadoId != null && v.EmpleadoId == userId))
                .OrderByDescending(v => v.Fecha)
                .ToListAsync();

            return View("~/Views/MisCompras/Index.cshtml", ventas);
        }

        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var userId = GetCurrentUserId();

            var venta = await _context.Ventas
                .AsNoTracking()
                .Include(v => v.Detalles)
                    .ThenInclude(d => d.Producto)
                .FirstOrDefaultAsync(v => v.VentaId == id && (v.ClienteId == userId || (v.EmpleadoId != null && v.EmpleadoId == userId)));

            if (venta == null)
                return NotFound();

            return View("~/Views/MisCompras/Details.cshtml", venta);
        }

        private int GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(userIdClaim, out var userId) ? userId : 0;
        }
    }
}