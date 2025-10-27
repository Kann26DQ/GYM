using GYM.Data;
using GYM.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GYM.Controllers
{
    [Authorize(Roles = "Cliente,Gymbro")]
    public class CartController : Controller
    {
        private readonly AppDBContext _context;

        public CartController(AppDBContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var cartItems = await _context.CartItems
                .Include(ci => ci.Producto)
                .Where(ci => ci.UsuarioId == GetCurrentUserId())
                .ToListAsync();

            ViewBag.Total = cartItems.Sum(ci => ci.Producto.Precio * ci.Cantidad);
            return View("~/Views/Cart/Index.cshtml", cartItems);
        }

        [HttpGet]
        public async Task<IActionResult> Productos()
        {
            var productos = await _context.Productos.ToListAsync();
            var cartItems = await _context.CartItems
                .Where(ci => ci.UsuarioId == GetCurrentUserId())
                .ToDictionaryAsync(ci => ci.ProductoId, ci => ci.Cantidad);

            ViewBag.CartItems = cartItems;
            return View("~/Views/Productos/Index.cshtml", productos);
        }

        [HttpGet]
        public async Task<IActionResult> CartMap()
        {
            var userId = GetCurrentUserId();
            var dict = await _context.CartItems
                .Where(ci => ci.UsuarioId == userId)
                .ToDictionaryAsync(ci => ci.ProductoId, ci => ci.Cantidad);

            return Json(dict);
        }

        // AGREGAR PRODUCTO AL CARRITO (limitado por StockVenta)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddToCart(int productoId, int cantidad = 1)
        {
            if (cantidad <= 0)
            {
                TempData["Error"] = "La cantidad debe ser mayor a 0.";
                return RedirectToAction("Productos");
            }

            var producto = await _context.Productos.FindAsync(productoId);
            if (producto == null)
            {
                TempData["Error"] = "El producto no existe.";
                return RedirectToAction("Productos");
            }

            var userId = GetCurrentUserId();
            var cartItem = await _context.CartItems
                .FirstOrDefaultAsync(ci => ci.ProductoId == productoId && ci.UsuarioId == userId);

            var enCarrito = cartItem?.Cantidad ?? 0;

            // Limitar por cupo de venta (StockVenta)
            var cupoVenta = Math.Max(0, producto.StockVenta);
            var maxAgregable = cupoVenta - enCarrito;
            if (maxAgregable < 0) maxAgregable = 0;

            if (cantidad > maxAgregable)
            {
                TempData["Error"] = $"Solo puedes añadir {maxAgregable} unidad(es) más de {producto.Nombre}.";
                return RedirectToAction("Productos");
            }

            if (cartItem != null)
            {
                cartItem.Cantidad += cantidad;
                _context.CartItems.Update(cartItem);
            }
            else
            {
                cartItem = new CartItem
                {
                    ProductoId = productoId,
                    Cantidad = cantidad,
                    UsuarioId = userId
                };
                await _context.CartItems.AddAsync(cartItem);
            }

            await _context.SaveChangesAsync();
            TempData["Success"] = "Producto añadido al carrito.";
            return RedirectToAction("Productos");
        }

        // ELIMINAR PRODUCTO DEL CARRITO
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Remove(int cartItemId)
        {
            var cartItem = await _context.CartItems
                .Include(ci => ci.Producto)
                .FirstOrDefaultAsync(ci => ci.CartItemId == cartItemId);

            if (cartItem == null || cartItem.UsuarioId != GetCurrentUserId())
            {
                TempData["Error"] = "El producto no está en tu carrito.";
                return RedirectToAction(nameof(Index));
            }

            _context.CartItems.Remove(cartItem);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Producto eliminado del carrito.";
            return RedirectToAction(nameof(Index));
        }

        // FINALIZAR COMPRA (descuenta StockVenta)
        [HttpGet]
        public async Task<IActionResult> Checkout()
        {
            var cartItems = await _context.CartItems
                .Include(ci => ci.Producto)
                .Where(ci => ci.UsuarioId == GetCurrentUserId())
                .ToListAsync();

            if (!cartItems.Any())
            {
                TempData["Error"] = "El carrito está vacío.";
                return RedirectToAction(nameof(Index));
            }

            ViewBag.Total = cartItems.Sum(ci => ci.Producto.Precio * ci.Cantidad);
            return View("~/Views/Cart/Checkout.cshtml", cartItems);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CheckoutConfirm()
        {
            var cartItems = await _context.CartItems
                .Include(ci => ci.Producto)
                .Where(ci => ci.UsuarioId == GetCurrentUserId())
                .ToListAsync();

            if (!cartItems.Any())
            {
                TempData["Error"] = "El carrito está vacío.";
                return RedirectToAction(nameof(Index));
            }

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var clienteId = GetCurrentUserId();
                int? empleadoId = null;
                if (User.IsInRole("Empleado")) empleadoId = clienteId;

                // Validar cupo en venta antes de descontar
                foreach (var item in cartItems)
                {
                    if (item.Producto.StockVenta < item.Cantidad)
                    {
                        TempData["Error"] = $"Cupo de venta insuficiente para el producto {item.Producto.Nombre}.";
                        return RedirectToAction(nameof(Index));
                    }
                }

                var venta = new Venta
                {
                    Fecha = DateTime.Now,
                    ClienteId = clienteId,
                    EmpleadoId = empleadoId,
                    Total = cartItems.Sum(ci => ci.Producto.Precio * ci.Cantidad),
                    Detalles = cartItems.Select(ci => new DetalleVenta
                    {
                        ProductoId = ci.ProductoId,
                        Cantidad = ci.Cantidad,
                        PrecioUnitario = ci.Producto.Precio
                    }).ToList()
                };
                _context.Ventas.Add(venta);

                // Descontar del cupo de venta (StockVenta)
                foreach (var item in cartItems)
                {
                    var p = await _context.Productos.FindAsync(item.ProductoId);
                    if (p == null) continue;

                    p.StockVenta -= item.Cantidad;
                    if (p.StockVenta < 0) p.StockVenta = 0;

                    // Disponible si aún hay cupo en venta
                    p.Disponible = p.StockVenta > 0;

                    _context.Productos.Update(p);
                }

                _context.CartItems.RemoveRange(cartItems);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                TempData["Success"] = "Compra realizada con éxito.";
                return RedirectToAction("Index", "Productos");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                TempData["Error"] = $"Error al procesar la compra: {ex.Message}.";
                return RedirectToAction(nameof(Index));
            }
        }

        private int GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(userIdClaim, out var userId) ? userId : 0;
        }
    }
}