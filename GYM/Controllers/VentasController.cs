using GYM.Data;
using GYM.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GYM.Controllers.SuperAdmin
{
    [Authorize(Roles = "SuperAdmin")]
    public class VentasController : Controller
    {
        private readonly AppDBContext _context;

        public VentasController(AppDBContext context)
        {
            _context = context;
        }

        // ------------------------------
        // LISTAR TODAS LAS VENTAS
        // ------------------------------
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var ventas = await _context.Ventas
                .Include(v => v.Cliente)
                .Include(v => v.Detalles)
                .ThenInclude(d => d.Producto)
                .ToListAsync();

            return View("~/Views/SuperAdmin/Ventas/Index.cshtml", ventas);
        }

        // ------------------------------
        // VER DETALLES DE UNA VENTA
        // ------------------------------
        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var venta = await _context.Ventas
                .Include(v => v.Cliente)
                .Include(v => v.Empleado)
                .Include(v => v.Detalles)
                .ThenInclude(d => d.Producto)
                .FirstOrDefaultAsync(v => v.VentaId == id);

            if (venta == null)
                return NotFound();

            return View("~/Views/SuperAdmin/Ventas/Details.cshtml", venta);
        }
    }
}