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
            return View("~/Views/Superadmin/GestionProveedor/Index.cshtml", proveedores);


        }

        // NUEVO - GET
        [HttpGet]
        public IActionResult Create()
        {
            return View("~/Views/Superadmin/GestionProveedor/Create.cshtml");
        }

        // NUEVO - POST
        [HttpPost]
        public async Task<IActionResult> Create(Proveedor proveedor)
        {
            if (string.IsNullOrWhiteSpace(proveedor.Nombre) ||
                string.IsNullOrWhiteSpace(proveedor.Telefono))
            {
                ViewBag.Mensaje = "Todos los campos son obligatorios.";
            
                return View("~/Views/Superadmin/GestionProveedor/Editar.cshtml", proveedor);

            }

            if (proveedor.Telefono == "000000000")
            {
                ViewBag.Mensaje = "El número de teléfono no puede ser 000000000.";
                return View("~/Views/Superadmin/GestionProveedor/Editar.cshtml", proveedor);
            }

            if (proveedor.Telefono.Length != 9 || !proveedor.Telefono.All(char.IsDigit))
            {
                ViewBag.Mensaje = "El número de teléfono debe tener exactamente 9 dígitos numéricos.";
                return View("~/Views/Superadmin/GestionProveedor/Editar.cshtml", proveedor);
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
            if (string.IsNullOrWhiteSpace(proveedor.Nombre) ||
                string.IsNullOrWhiteSpace(proveedor.Telefono))
            {
                ViewBag.Mensaje = "Todos los campos son obligatorios.";
                return View("~/Views/SuperAdmin/GestionProveedor/Edit.cshtml", proveedor);
            }

            if (proveedor.Telefono == "000000000")
            {
                ViewBag.Mensaje = "El número de teléfono no puede ser 000000000.";
                return View("~/Views/SuperAdmin/GestionProveedor/Edit.cshtml", proveedor);
            }

            if (proveedor.Telefono.Length != 9 || !proveedor.Telefono.All(char.IsDigit))
            {
                ViewBag.Mensaje = "El número de teléfono debe tener exactamente 9 dígitos numéricos.";
                return View("~/Views/SuperAdmin/GestionProveedor/Edit.cshtml", proveedor);
            }

            var existente = await _context.Proveedores.FindAsync(proveedor.ProveedorId);
            if (existente == null)
                return NotFound();

            _context.Entry(existente).CurrentValues.SetValues(proveedor);
            await _context.SaveChangesAsync();
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
