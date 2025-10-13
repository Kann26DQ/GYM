using GYM.Data;
using GYM.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GYM.Controllers.SuperAdmin
{
    public class GestionStockController : Controller
    {
        private readonly AppDBContext _context;

        public GestionStockController(AppDBContext context)
        {
            _context = context;
        }

        // ------------------------------
        // LISTAR PRODUCTOS
        // ------------------------------
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var productos = await _context.Productos
                .Include(p => p.Proveedor)
                .OrderBy(p => p.Nombre)
                .ToListAsync();

            return View("~/Views/SuperAdmin/GestionStock/Index.cshtml", productos);
        }

        // ------------------------------
        // CREAR - GET
        // ------------------------------
        [HttpGet]
        public IActionResult Create()
        {
            ViewBag.Proveedores = _context.Proveedores.ToList();
            return View("~/Views/SuperAdmin/GestionStock/Create.cshtml");
        }

        // ------------------------------
        // CREAR - POST
        // ------------------------------
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Producto producto)
        {
            ViewBag.Proveedores = _context.Proveedores.ToList();

            // Validaciones simples
            if (string.IsNullOrWhiteSpace(producto.Nombre) || producto.Stock < 0)
            {
                ViewBag.Mensaje = "Todos los campos son obligatorios y el stock no puede ser negativo.";
                return View("~/Views/SuperAdmin/GestionStock/Create.cshtml", producto);
            }

            try
            {
                await _context.Productos.AddAsync(producto);
                await _context.SaveChangesAsync();

                TempData["Success"] = "Producto registrado correctamente.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                ViewBag.Mensaje = "Hubo un error al registrar el producto: " + ex.Message;
                return View("~/Views/SuperAdmin/GestionStock/Create.cshtml", producto);
            }
        }

        // ------------------------------
        // EDITAR - GET
        // ------------------------------
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var producto = await _context.Productos.FindAsync(id);
            if (producto == null)
                return NotFound();

            ViewBag.Proveedores = _context.Proveedores.ToList();
            return View("~/Views/SuperAdmin/GestionStock/Edit.cshtml", producto);
        }

        // ------------------------------
        // EDITAR - POST
        // ------------------------------
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Producto producto)
        {
            if (id != producto.ProductoId)
                return NotFound();

            ViewBag.Proveedores = _context.Proveedores.ToList();

            if (string.IsNullOrWhiteSpace(producto.Nombre) || producto.Stock < 0)
            {
                ViewBag.Mensaje = "Verifica los datos ingresados.";
                return View("~/Views/SuperAdmin/GestionStock/Edit.cshtml", producto);
            }

            try
            {
                _context.Update(producto);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Producto actualizado correctamente.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                ViewBag.Mensaje = "Error al actualizar el producto: " + ex.Message;
                return View("~/Views/SuperAdmin/GestionStock/Edit.cshtml", producto);
            }
        }

        // ------------------------------
        // ELIMINAR
        // ------------------------------
        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            var producto = await _context.Productos
                .Include(p => p.Proveedor)
                .FirstOrDefaultAsync(p => p.ProductoId == id);

            if (producto == null)
                return NotFound();

            return View("~/Views/SuperAdmin/GestionStock/Delete.cshtml", producto);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var producto = await _context.Productos.FindAsync(id);
            if (producto != null)
            {
                _context.Productos.Remove(producto);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Producto eliminado correctamente.";
            }
            return RedirectToAction(nameof(Index));
        }

        // ------------------------------
        // HISTORIAL
        // ------------------------------
        [HttpGet]
        public async Task<IActionResult> Historial()
        {
            var movimientos = await _context.MovimientosStock
                .Include(m => m.Producto)
                .Include(m => m.Empleado)
                .OrderByDescending(m => m.Fecha)
                .ToListAsync();

            return View("~/Views/SuperAdmin/GestionStock/Historial.cshtml", movimientos);
        }
    }
}
