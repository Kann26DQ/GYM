using GYM.Data;
using GYM.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GYM.Controllers
{
    public class ProductosController : Controller
    {
        private readonly AppDBContext _context;
        public ProductosController(AppDBContext context) => _context = context;

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var productos = await _context.Productos
                .Include(p => p.Proveedor)
                .Where(p => p.Disponible)
                .OrderBy(p => p.Nombre)
                .ToListAsync();

            return View("~/Views/Productos/Index.cshtml", productos);
        }

        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var producto = await _context.Productos
                .Include(p => p.Proveedor)
                .Include(p => p.Movimientos)
                .FirstOrDefaultAsync(p => p.ProductoId == id && p.Disponible);

            if (producto == null) return NotFound();
            return View("~/Views/Productos/Details.cshtml", producto);
        }
    }
}