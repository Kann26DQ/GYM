using GYM.Data;
using GYM.Models;
using GYM.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;

namespace GYM.Controllers
{
    public class GestionUsuariosController : Controller
    {
        private readonly AppDBContext _context;
        private readonly PasswordHasher<Usuario> _hasher;

        public GestionUsuariosController(AppDBContext context)
        {
            _context = context;
            _hasher = new PasswordHasher<Usuario>();
        }

        // GET: /GestionUsuarios/
        public async Task<IActionResult> Index()
        {
            // Solo el SuperAdmin puede acceder
            var rolSesion = HttpContext.Session.GetString("Rol");
            if (rolSesion != null && rolSesion != "SuperAdmin")
                return Forbid();

            var usuarios = await _context.Usuarios
                .Include(u => u.Rol)
                .Where(u => (u.RolId == 1 || u.RolId == 2))
                .OrderBy(u => u.RolId)
                .ThenBy(u => u.Nombre)
                .ToListAsync();

            return View("~/Views/SuperAdmin/GestionUsuarios/Index.cshtml", usuarios); 
        }

        // GET: /GestionUsuarios/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var usuario = await _context.Usuarios
                .Include(u => u.Rol)
                .FirstOrDefaultAsync(u => u.UsuarioId == id);

            if (usuario == null) return NotFound();
            return View("~/Views/SuperAdmin/GestionUsuarios/Details.cshtml", usuario);
        }

        // GET: /GestionUsuarios/Create
        public IActionResult Create()
        {
            ViewBag.Roles = _context.Roles.Where(r => r.RolId == 1 || r.RolId == 2).ToList();
            return View("~/Views/SuperAdmin/GestionUsuarios/Create.cshtml");
        }

        // POST: /GestionUsuarios/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(UsuarioCreateVM model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Roles = _context.Roles.Where(r => r.RolId == 1 || r.RolId == 2).ToList();
                return View("~/Views/SuperAdmin/GestionUsuarios/Create.cshtml", model);
            }

            // Validar email único
            bool exists = await _context.Usuarios.AnyAsync(u => u.Email == model.Email);
            if (exists)
            {
                ModelState.AddModelError("Email", "El email ya está registrado.");
                ViewBag.Roles = _context.Roles.Where(r => r.RolId == 1 || r.RolId == 2).ToList();
                return View("~/Views/SuperAdmin/GestionUsuarios/Create.cshtml", model);
            }

            var usuario = new Usuario
            {
                Nombre = model.Nombre,
                Email = model.Email,
                Telefono = model.Telefono,
                FechaRegistro = DateTime.Now,
                RolId = model.RolId,
                Activo = true
            };

            // Contraseña con hash
            usuario.Password = _hasher.HashPassword(usuario, model.Password);

            _context.Usuarios.Add(usuario);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        // GET: /GestionUsuarios/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var usuario = await _context.Usuarios.FindAsync(id);
            if (usuario == null) return NotFound();

            var model = new UsuarioEditVM
            {
                UsuarioId = usuario.UsuarioId,
                Nombre = usuario.Nombre,
                Email = usuario.Email,
                Telefono = usuario.Telefono,
                RolId = usuario.RolId,
                Activo = usuario.Activo
            };

            ViewBag.Roles = _context.Roles.Where(r => r.RolId == 1 || r.RolId == 2).ToList();
            return View("~/Views/SuperAdmin/GestionUsuarios/Edit.cshtml", model);
        }

        // POST: /GestionUsuarios/Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(UsuarioEditVM model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Roles = _context.Roles.Where(r => r.RolId == 1 || r.RolId == 2).ToList();
                return View("~/Views/SuperAdmin/GestionUsuarios/Edit.cshtml", model);
            }

            var usuario = await _context.Usuarios.FindAsync(model.UsuarioId);
            if (usuario == null) return NotFound();

            usuario.Nombre = model.Nombre;
            usuario.Email = model.Email;
            usuario.Telefono = model.Telefono;
            usuario.RolId = model.RolId;
            usuario.Activo = model.Activo;

            // Re-hash si hay nueva contraseña
            if (!string.IsNullOrWhiteSpace(model.NewPassword))
                usuario.Password = _hasher.HashPassword(usuario, model.NewPassword);

            _context.Usuarios.Update(usuario);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        // GET: /GestionUsuarios/Delete/5
        public async Task<IActionResult> Delete(int id)
        {
            var usuario = await _context.Usuarios
                .Include(u => u.Rol)
                .FirstOrDefaultAsync(u => u.UsuarioId == id);

            if (usuario == null) return NotFound();

            return View("~/Views/SuperAdmin/GestionUsuarios/Delete.cshtml", usuario);
        }

        // POST: /GestionUsuarios/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var usuario = await _context.Usuarios.FindAsync(id);
            if (usuario == null) return NotFound();

            // Soft delete (no se elimina el registro)
            usuario.Activo = false;

            _context.Usuarios.Update(usuario);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        // POST: /GestionUsuarios/ToggleActive
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleActivo(int id)
        {
            var usuario = await _context.Usuarios.FindAsync(id);
            if (usuario == null) return NotFound();

            usuario.Activo = !usuario.Activo;
            _context.Usuarios.Update(usuario);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }
    }
}
