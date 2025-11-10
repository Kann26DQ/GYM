using GYM.Data;
using GYM.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GYM.Controllers.SuperAdmin
{
    public class GestionProveedorController : Controller
    {
        private readonly AppDBContext _context;

        public GestionProveedorController(AppDBContext context)
        {
            _context = context;
        }

        // LISTA
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var proveedores = await _context.Proveedores.ToListAsync();
            return View("~/Views/SuperAdmin/GestionProveedor/Index.cshtml", proveedores);
        }

        // NUEVO - GET
        [HttpGet]
        public IActionResult Create()
        {
            return View("~/Views/SuperAdmin/GestionProveedor/Create.cshtml");
        }

        // NUEVO - POST
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Proveedor proveedor)
        {
            if (string.IsNullOrWhiteSpace(proveedor.Nombre) ||
                string.IsNullOrWhiteSpace(proveedor.Telefono))
            {
                ViewBag.Mensaje = "Todos los campos son obligatorios.";
                return View("~/Views/SuperAdmin/GestionProveedor/Create.cshtml", proveedor);
            }

            if (proveedor.Telefono == "000000000")
            {
                ViewBag.Mensaje = "El número de teléfono no puede ser 000000000.";
                return View("~/Views/SuperAdmin/GestionProveedor/Create.cshtml", proveedor);
            }

            if (proveedor.Telefono.Length != 9 || !proveedor.Telefono.All(char.IsDigit))
            {
                ViewBag.Mensaje = "El número de teléfono debe tener exactamente 9 dígitos numéricos.";
                return View("~/Views/SuperAdmin/GestionProveedor/Create.cshtml", proveedor);
            }

            await _context.Proveedores.AddAsync(proveedor);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // EDITAR - GET
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var proveedor = await _context.Proveedores.FindAsync(id);
            if (proveedor == null)
                return NotFound();

            return View("~/Views/SuperAdmin/GestionProveedor/Edit.cshtml", proveedor);
        }

        // EDITAR - POST
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Proveedor proveedor)
        {

            // Normaliza
            proveedor.Nombre = proveedor.Nombre?.Trim() ?? string.Empty;
            proveedor.Telefono = proveedor.Telefono?.Trim() ?? string.Empty;
            proveedor.Email = proveedor.Email?.Trim() ?? string.Empty;
            proveedor.Direccion = string.IsNullOrWhiteSpace(proveedor.Direccion) ? null : proveedor.Direccion.Trim();

            if (!ModelState.IsValid)
                return View("~/Views/SuperAdmin/GestionProveedor/Create.cshtml", proveedor);

            _context.Proveedores.Add(proveedor);
            await _context.SaveChangesAsync();
            TempData["Success"] = "Proveedor registrado correctamente.";
            return RedirectToAction(nameof(Index));
        }

        // ELIMINAR
        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            var proveedor = await _context.Proveedores.FindAsync(id);
            if (proveedor == null)
                return NotFound();

            _context.Proveedores.Remove(proveedor);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
    }
}