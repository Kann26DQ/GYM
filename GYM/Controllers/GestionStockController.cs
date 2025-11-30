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

        /// <summary>
        /// ✅ NUEVO: Index con filtro de búsqueda
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Index(string buscar, string tipoFiltro = "nombre")
        {
            var query = _context.Productos
                .Include(p => p.Proveedor)
                .AsQueryable();

            // ✅ Aplicar filtros de búsqueda
            if (!string.IsNullOrWhiteSpace(buscar))
            {
                buscar = buscar.Trim();

                switch (tipoFiltro.ToLower())
                {
                    case "nombre":
                        query = query.Where(p => p.Nombre.ToLower().Contains(buscar.ToLower()));
                        break;
                    case "proveedor":
                        query = query.Where(p => p.Proveedor != null && p.Proveedor.Nombre.ToLower().Contains(buscar.ToLower()));
                        break;
                    case "stock":
                        if (int.TryParse(buscar, out int stockBuscado) && stockBuscado >= 0)
                        {
                            query = query.Where(p => p.Stock == stockBuscado);
                        }
                        break;
                    case "precio":
                        if (decimal.TryParse(buscar, out decimal precioBuscado) && precioBuscado >= 0)
                        {
                            query = query.Where(p => p.Precio == precioBuscado);
                        }
                        break;
                    default:
                        // Búsqueda general en todos los campos
                        query = query.Where(p =>
                            p.Nombre.ToLower().Contains(buscar.ToLower()) ||
                            (p.Proveedor != null && p.Proveedor.Nombre.ToLower().Contains(buscar.ToLower())) ||
                            p.Descripcion.ToLower().Contains(buscar.ToLower()));
                        break;
                }
            }

            var productos = await query
                .OrderBy(p => p.Nombre)
                .ToListAsync();

            // Agotado por nombre: solo si todos los registros con ese nombre tienen stock 0
            var productosAgotados = productos
                .GroupBy(p => p.Nombre)
                .Where(g => g.All(x => x.Stock <= 0))
                .Select(g => g.First())
                .ToList();

            ViewBag.ProductosAgotados = productosAgotados;
            ViewBag.ProductosBajoStock = productos.Where(p => p.Stock > 0 && p.Stock <= 5).ToList();

            // Pasar valores al ViewBag para mantenerlos en la vista
            ViewBag.BuscarActual = buscar;
            ViewBag.TipoFiltroActual = tipoFiltro;

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
            producto.Descripcion = producto.Descripcion ?? string.Empty;
            producto.FechaRegistro = DateTime.Now;
            producto.StockVenta = 0;                // no se expone nada por defecto
            producto.Disponible = producto.StockVenta > 0;

            if (string.IsNullOrWhiteSpace(producto.Nombre))
                ModelState.AddModelError(nameof(producto.Nombre), "El nombre es obligatorio.");
            if (producto.Precio < 0)
                ModelState.AddModelError(nameof(producto.Precio), "El precio no puede ser negativo.");
            if (producto.Stock < 0)
                ModelState.AddModelError(nameof(producto.Stock), "El stock no puede ser negativo.");

            if (!ModelState.IsValid)
            {
                ViewBag.Mensaje = "Corrige los errores del formulario.";
                return View("~/Views/SuperAdmin/GestionStock/Create.cshtml", producto);
            }

            // Asegurar campos no nulos que la BD requiere
            producto.Descripcion = producto.Descripcion ?? string.Empty;
            producto.FechaRegistro = DateTime.Now;

            var isAuth = User.Identity?.IsAuthenticated ?? false;
            var isRole = User.IsInRole("SuperAdmin");
            var claims = string.Join(" | ", User.Claims.Select(c => $"{c.Type}={c.Value}"));
            if (!isAuth || !isRole)
            {
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
        // EDITAR - POST: valida precio y stock no negativos
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Producto producto)
        {
            if (id != producto.ProductoId)
                return NotFound();

            ViewBag.Proveedores = _context.Proveedores.ToList();

            if (string.IsNullOrWhiteSpace(producto.Nombre))
                ModelState.AddModelError(nameof(producto.Nombre), "El nombre es obligatorio.");
            if (producto.Precio < 0)
                ModelState.AddModelError(nameof(producto.Precio), "El precio no puede ser negativo.");
            if (producto.Stock < 0)
                ModelState.AddModelError(nameof(producto.Stock), "El stock no puede ser negativo.");

            if (!ModelState.IsValid)
            {
                ViewBag.Mensaje = "Verifica los datos ingresados.";
                return View("~/Views/SuperAdmin/GestionStock/Edit.cshtml", producto);
            }

            try
            {
                // coherencia: disponible solo si hay stock
                producto.Disponible = producto.Stock > 0;

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
                var nuevoStock = tipoMovimiento == "Entrada"
                    ? producto.Stock + cantidad
                    : producto.Stock - cantidad;

                producto.Stock = Math.Max(0, nuevoStock);
                producto.Disponible = producto.Stock > 0;

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

        [HttpPost]
        [Authorize(Roles = "SuperAdmin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReconciliarStockDesdeCarritos()
        {
            // Suma de cantidades por producto que actualmente están en carritos
            var enCarritos = await _context.CartItems
                .GroupBy(ci => ci.ProductoId)
                .Select(g => new { ProductoId = g.Key, Cantidad = g.Sum(x => x.Cantidad) })
                .ToListAsync();

            foreach (var g in enCarritos)
            {
                var p = await _context.Productos.FindAsync(g.ProductoId);
                if (p == null) continue;

                // Devuelve al stock lo retenido en carritos (pasado)
                p.Stock += g.Cantidad;

                // Si hay stock, márcalo disponible
                if (p.Stock > 0) p.Disponible = true;

                _context.Productos.Update(p);
            }

            await _context.SaveChangesAsync();
            TempData["Success"] = "Stock reconciliado desde carritos. A partir de ahora el stock solo se descuenta al comprar.";
            return RedirectToAction(nameof(Index));
        }

        // NUEVO: ajustar cupo de venta (poner/retirar)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AjustarStockVenta(int id, string accion, int cantidad)
        {
            if (cantidad <= 0) { TempData["Error"] = "La cantidad debe ser mayor a 0."; return RedirectToAction(nameof(Index)); }

            var producto = await _context.Productos.FindAsync(id);
            if (producto == null) return NotFound();

            accion = (accion ?? "").Trim().ToLowerInvariant();
            if (accion == "poner")
            {
                if (cantidad > producto.Stock)
                {
                    TempData["Error"] = $"Solo puedes poner hasta {producto.Stock} unidad(es).";
                    return RedirectToAction(nameof(Index));
                }
                producto.Stock -= cantidad;          // sale de bodega
                producto.StockVenta += cantidad;     // entra al cupo en venta
            }
            else if (accion == "retirar")
            {
                if (cantidad > producto.StockVenta)
                {
                    TempData["Error"] = $"Solo puedes retirar hasta {producto.StockVenta} unidad(es).";
                    return RedirectToAction(nameof(Index));
                }
                producto.Stock += cantidad;          // vuelve a bodega
                producto.StockVenta -= cantidad;     // sale del cupo
            }
            else
            {
                TempData["Error"] = "Acción inválida.";
                return RedirectToAction(nameof(Index));
            }

            producto.Disponible = producto.StockVenta > 0;
            _context.Productos.Update(producto);
            await _context.SaveChangesAsync();

            TempData["Success"] = accion == "poner"
                ? $"Pusiste {cantidad} a la venta. Stock: {producto.Stock}, En venta: {producto.StockVenta}."
                : $"Retiraste {cantidad} de la venta. Stock: {producto.Stock}, En venta: {producto.StockVenta}.";
            return RedirectToAction(nameof(Index));
        }
    }
}