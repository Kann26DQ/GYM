using GYM.Data;
using GYM.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GYM.Controllers.SuperAdmin
{
    [Authorize(Roles = "SuperAdmin")]
    public class GestionProveedorController : Controller
    {
        private readonly AppDBContext _context;

        public GestionProveedorController(AppDBContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Index con filtro de búsqueda
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Index(string buscar, string tipoFiltro = "nombre")
        {
            var query = _context.Proveedores.AsQueryable();

            if (!string.IsNullOrWhiteSpace(buscar))
            {
                buscar = buscar.Trim();

                switch (tipoFiltro.ToLower())
                {
                    case "nombre":
                        query = query.Where(p => p.Nombre.ToLower().Contains(buscar.ToLower()));
                        break;
                    case "telefono":
                        query = query.Where(p => p.Telefono.Contains(buscar));
                        break;
                    case "email":
                        query = query.Where(p => p.Email.ToLower().Contains(buscar.ToLower()));
                        break;
                    case "direccion":
                        query = query.Where(p => p.Direccion != null && p.Direccion.ToLower().Contains(buscar.ToLower()));
                        break;
                    default:
                        query = query.Where(p =>
                            p.Nombre.ToLower().Contains(buscar.ToLower()) ||
                            p.Telefono.Contains(buscar) ||
                            p.Email.ToLower().Contains(buscar.ToLower()) ||
                            (p.Direccion != null && p.Direccion.ToLower().Contains(buscar.ToLower())));
                        break;
                }
            }

            var proveedores = await query
                .OrderBy(p => p.Nombre)
                .ToListAsync();

            ViewBag.BuscarActual = buscar;
            ViewBag.TipoFiltroActual = tipoFiltro;

            return View("~/Views/SuperAdmin/GestionProveedor/Index.cshtml", proveedores);
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View("~/Views/SuperAdmin/GestionProveedor/Create.cshtml");
        }

        /// <summary>
        /// ✅ NUEVO - POST CON VALIDACIONES DE DUPLICADOS
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Proveedor proveedor)
        {
            // ✅ Normalizar datos ANTES de validar
            proveedor.Nombre = proveedor.Nombre?.Trim() ?? string.Empty;
            proveedor.Telefono = proveedor.Telefono?.Trim() ?? string.Empty;
            proveedor.Email = proveedor.Email?.Trim().ToLower() ?? string.Empty;
            proveedor.Direccion = string.IsNullOrWhiteSpace(proveedor.Direccion) ? null : proveedor.Direccion.Trim();

            // ✅ VALIDACIÓN 1: Verificar si el EMAIL ya existe
            var emailExiste = await _context.Proveedores
                .AnyAsync(p => p.Email.ToLower() == proveedor.Email.ToLower());

            if (emailExiste)
            {
                ModelState.AddModelError(nameof(proveedor.Email), "Ya existe un proveedor registrado con este correo electrónico.");
            }

            // ✅ VALIDACIÓN 2: Verificar si el TELÉFONO ya existe
            var telefonoExiste = await _context.Proveedores
                .AnyAsync(p => p.Telefono == proveedor.Telefono);

            if (telefonoExiste)
            {
                ModelState.AddModelError(nameof(proveedor.Telefono), "Ya existe un proveedor registrado con este número de teléfono.");
            }

            // ✅ VALIDACIÓN 3: Teléfono no puede ser 000000000
            if (proveedor.Telefono == "000000000")
            {
                ModelState.AddModelError(nameof(proveedor.Telefono), "El número de teléfono no puede ser 000000000.");
            }

            // ✅ VALIDACIÓN 4: Teléfono debe tener exactamente 9 dígitos
            if (!string.IsNullOrEmpty(proveedor.Telefono) &&
                (proveedor.Telefono.Length != 9 || !proveedor.Telefono.All(char.IsDigit)))
            {
                ModelState.AddModelError(nameof(proveedor.Telefono), "El número de teléfono debe tener exactamente 9 dígitos numéricos.");
            }

            // Si hay errores de validación, volver a la vista
            if (!ModelState.IsValid)
            {
                return View("~/Views/SuperAdmin/GestionProveedor/Create.cshtml", proveedor);
            }

            try
            {
                await _context.Proveedores.AddAsync(proveedor);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Proveedor registrado correctamente.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Error al registrar el proveedor: {ex.Message}");
                return View("~/Views/SuperAdmin/GestionProveedor/Create.cshtml", proveedor);
            }
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var proveedor = await _context.Proveedores.FindAsync(id);
            if (proveedor == null)
                return NotFound();

            return View("~/Views/SuperAdmin/GestionProveedor/Edit.cshtml", proveedor);
        }

        /// <summary>
        /// ✅ EDITAR - POST CON VALIDACIONES DE DUPLICADOS
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Proveedor proveedor)
        {
            // ✅ Normalizar datos ANTES de validar
            proveedor.Nombre = proveedor.Nombre?.Trim() ?? string.Empty;
            proveedor.Telefono = proveedor.Telefono?.Trim() ?? string.Empty;
            proveedor.Email = proveedor.Email?.Trim().ToLower() ?? string.Empty;
            proveedor.Direccion = string.IsNullOrWhiteSpace(proveedor.Direccion) ? null : proveedor.Direccion.Trim();

            // ✅ VALIDACIÓN 1: Verificar si el EMAIL ya existe (excluyendo el actual)
            var emailExiste = await _context.Proveedores
                .AnyAsync(p => p.Email.ToLower() == proveedor.Email.ToLower() && p.ProveedorId != proveedor.ProveedorId);

            if (emailExiste)
            {
                ModelState.AddModelError(nameof(proveedor.Email), "Ya existe otro proveedor registrado con este correo electrónico.");
            }

            // ✅ VALIDACIÓN 2: Verificar si el TELÉFONO ya existe (excluyendo el actual)
            var telefonoExiste = await _context.Proveedores
                .AnyAsync(p => p.Telefono == proveedor.Telefono && p.ProveedorId != proveedor.ProveedorId);

            if (telefonoExiste)
            {
                ModelState.AddModelError(nameof(proveedor.Telefono), "Ya existe otro proveedor registrado con este número de teléfono.");
            }

            // ✅ VALIDACIÓN 3: Teléfono no puede ser 000000000
            if (proveedor.Telefono == "000000000")
            {
                ModelState.AddModelError(nameof(proveedor.Telefono), "El número de teléfono no puede ser 000000000.");
            }

            // ✅ VALIDACIÓN 4: Teléfono debe tener exactamente 9 dígitos
            if (!string.IsNullOrEmpty(proveedor.Telefono) &&
                (proveedor.Telefono.Length != 9 || !proveedor.Telefono.All(char.IsDigit)))
            {
                ModelState.AddModelError(nameof(proveedor.Telefono), "El número de teléfono debe tener exactamente 9 dígitos numéricos.");
            }

            if (!ModelState.IsValid)
            {
                return View("~/Views/SuperAdmin/GestionProveedor/Edit.cshtml", proveedor);
            }

            try
            {
                _context.Proveedores.Update(proveedor);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Proveedor actualizado correctamente.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Error al actualizar el proveedor: {ex.Message}");
                return View("~/Views/SuperAdmin/GestionProveedor/Edit.cshtml", proveedor);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var proveedor = await _context.Proveedores.FindAsync(id);
            if (proveedor == null)
            {
                TempData["Error"] = "Proveedor no encontrado.";
                return RedirectToAction(nameof(Index));
            }

            // Verificar si tiene productos asociados
            var tieneProductos = await _context.Productos
                .AnyAsync(p => p.ProveedorId == id);

            if (tieneProductos)
            {
                TempData["Error"] = "No se puede eliminar el proveedor porque tiene productos asociados.";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                _context.Proveedores.Remove(proveedor);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Proveedor eliminado correctamente.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error al eliminar el proveedor: {ex.Message}";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}