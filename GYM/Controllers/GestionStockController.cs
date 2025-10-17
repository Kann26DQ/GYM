using GYM.Data;
using GYM.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GYM.Controllers.SuperAdmin
{
    [Authorize(Roles = "SuperAdmin")]
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

            // Asegurar campos no nulos que la BD requiere
            producto.Descripcion = producto.Descripcion ?? string.Empty;
            producto.FechaRegistro = DateTime.Now;

            // DEBUG TEMPORAL: inspeccionar autenticación y claims en POST
            var isAuth = User.Identity?.IsAuthenticated ?? false;
            var isRole = User.IsInRole("SuperAdmin");
            var claims = string.Join(" | ", User.Claims.Select(c => $"{c.Type}={c.Value}"));
            if (!isAuth || !isRole)
            {
                // devuelve información para que la inspecciones en el navegador / Network
                return Content($"POST debug - IsAuthenticated={isAuth}\nIsInRoleSuperAdmin={isRole}\nClaims={claims}");
            }

            try
            {
                await _context.Productos.AddAsync(producto);
                await _context.SaveChangesAsync();

                var movimiento = new MovimientoStock
                {
                    ProductoId = producto.ProductoId,
                    Fecha = DateTime.Now,
                    TipoMovimiento = "Entrada",
                    Cantidad = producto.Stock,
                };

                // si tienes claim NameIdentifier, rellena UsuarioId
                var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (int.TryParse(userIdClaim, out int usuarioId))
                    movimiento.UsuarioId = usuarioId;

                await _context.MovimientosStock.AddAsync(movimiento);
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

        // ------------------------------
        // HISTORIAL
        // ------------------------------
        [HttpGet]
        public async Task<IActionResult> Historial(int? productoId = null)
        {
            var query = _context.MovimientosStock
                .Include(m => m.Producto)
                .Include(m => m.Usuario)
                .AsQueryable();

            if (productoId.HasValue)
            {
                query = query.Where(m => m.ProductoId == productoId.Value);
            }
            else
            {
               
                query = query.Where(m => m.TipoMovimiento == "Entrada");
            }

            var movimientos = await query
                .OrderByDescending(m => m.Fecha)
                .ToListAsync();

            return View("~/Views/SuperAdmin/GestionStock/Historial.cshtml", movimientos);
        }


        // ------------------------------
        // REGISTRAR MOVIMIENTO - GET (muestra formulario)
        // ------------------------------
        [HttpGet]
        public IActionResult RegistrarMovimiento(int productoId)
        {
            var producto = _context.Productos.Find(productoId);
            if (producto == null) return NotFound();
            ViewBag.Producto = producto;
            return View("~/Views/SuperAdmin/GestionStock/RegistrarMovimiento.cshtml");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RegistrarMovimiento(int productoId, string tipoMovimiento, int cantidad)
        {
            if (cantidad <= 0)
            {
                TempData["Error"] = "Cantidad inválida.";
                return RedirectToAction("Details", new { id = productoId });
            }

            tipoMovimiento = (tipoMovimiento ?? "").Trim();
            if (tipoMovimiento != "Entrada" && tipoMovimiento != "Salida")
            {
                TempData["Error"] = "Tipo de movimiento inválido.";
                return RedirectToAction("Details", new { id = productoId });
            }

            var producto = await _context.Productos.FindAsync(productoId);
            if (producto == null) return NotFound();

            if (tipoMovimiento == "Salida" && producto.Stock < cantidad)
            {
                TempData["Error"] = "Stock insuficiente.";
                return RedirectToAction("Details", new { id = productoId });
            }

            using var tx = await _context.Database.BeginTransactionAsync();
            try
            {
                producto.Stock = tipoMovimiento == "Entrada" ? producto.Stock + cantidad : producto.Stock - cantidad;
                _context.Productos.Update(producto);

                var movimiento = new MovimientoStock
                {
                    ProductoId = producto.ProductoId,
                    Fecha = DateTime.Now,
                    TipoMovimiento = tipoMovimiento,
                    Cantidad = cantidad,
                };

                var uid = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (int.TryParse(uid, out var usuarioId)) movimiento.UsuarioId = usuarioId;

                await _context.MovimientosStock.AddAsync(movimiento);
                await _context.SaveChangesAsync();

                await tx.CommitAsync();

                TempData["Success"] = "Movimiento registrado correctamente.";
                return RedirectToAction("Details", new { id = productoId });
            }
            catch
            {
                await tx.RollbackAsync();
                TempData["Error"] = "Error al procesar el movimiento.";
                return RedirectToAction("Details", new { id = productoId });
            }
        }
        // ------------------------------
        // DETALLES - GET
        // ------------------------------
        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var producto = await _context.Productos
                .Include(p => p.Proveedor)
                .Include(p => p.Movimientos)
                    .ThenInclude(m => m.Usuario) // cargar usuario dentro de cada movimiento
                .FirstOrDefaultAsync(p => p.ProductoId == id);

            if (producto == null)
                return NotFound();

            return View("~/Views/SuperAdmin/GestionStock/Details.cshtml", producto);
        }

        // Añade dentro de la clase GestionStockController (ya tienes [Authorize(Roles = "SuperAdmin")])
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleDisponibilidad(int id)
        {
            var producto = await _context.Productos.FindAsync(id);
            if (producto == null) return NotFound();

            producto.Disponible = !producto.Disponible;
            _context.Productos.Update(producto);
            await _context.SaveChangesAsync();

            TempData["Success"] = producto.Disponible
                ? $"Producto '{producto.Nombre}' puesto a la venta."
                : $"Producto '{producto.Nombre}' retirado de la venta.";

            return RedirectToAction(nameof(Index));
        }
    }
}
