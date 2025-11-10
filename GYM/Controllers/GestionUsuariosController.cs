using GYM.Data;
using GYM.Models;
using GYM.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;

namespace GYM.Controllers
{
    [Authorize(Roles = "SuperAdmin")]
    public class GestionUsuariosController : Controller
    {
        private readonly AppDBContext _context;
        private readonly PasswordHasher<Usuario> _hasher;

        public GestionUsuariosController(AppDBContext context)
        {
            _context = context;
            _hasher = new PasswordHasher<Usuario>();
        }

        // Validación remota: Email único
        [AcceptVerbs("GET", "POST")]
        public async Task<IActionResult> CheckEmailUnique(string email, int? usuarioId)
        {
            if (string.IsNullOrWhiteSpace(email)) return Json(true);

            var exists = await _context.Usuarios
                .AsNoTracking()
                .AnyAsync(u => u.Email.ToLower() == email.ToLower() && (usuarioId == null || u.UsuarioId != usuarioId.Value));

            return Json(!exists);
        }

        // Validación remota para Teléfono único
        [AcceptVerbs("GET", "POST")]
        public async Task<IActionResult> CheckTelefonoUnique(string telefono, int? usuarioId)
        {
            if (string.IsNullOrWhiteSpace(telefono)) return Json(true);

            var exists = await _context.Usuarios
                .AsNoTracking()
                .AnyAsync(u => u.Telefono == telefono && (usuarioId == null || u.UsuarioId != usuarioId.Value));

            return Json(!exists);
        }

        public async Task<IActionResult> Index()
        {
            var usuarios = await _context.Usuarios
                .Include(u => u.Rol)
                .Where(u => (u.RolId == 1 || u.RolId == 2))
                .OrderBy(u => u.RolId)
                .ThenBy(u => u.Nombre)
                .ToListAsync();

            return View("~/Views/SuperAdmin/GestionUsuarios/Index.cshtml", usuarios);
        }

        public async Task<IActionResult> Details(int id)
        {
            var usuario = await _context.Usuarios
                .Include(u => u.Rol)
                .FirstOrDefaultAsync(u => u.UsuarioId == id);

            if (usuario == null) return NotFound();

            var now = DateTime.UtcNow;
            var membresiaActiva = await _context.MembresiasUsuarios
                .Include(m => m.Plan)
                .Where(m => m.UsuarioId == id && m.Activa && m.FechaInicio <= now && m.FechaFin >= now)
                .OrderByDescending(m => m.FechaInicio)
                .FirstOrDefaultAsync();

            ViewData["MembresiaActiva"] = membresiaActiva;
            return View("~/Views/SuperAdmin/GestionUsuarios/Details.cshtml", usuario);
        }

        public async Task<IActionResult> Create()
        {
            ViewBag.Roles = _context.Roles.Where(r => r.RolId == 1 || r.RolId == 2).ToList();

            // 👇 NUEVO: Cargar planes de membresía activos
            ViewBag.Planes = await _context.MembresiaPlanes
                .Where(p => p.Activo)
                .OrderBy(p => p.Precio)
                .ToListAsync();

            return View("~/Views/SuperAdmin/GestionUsuarios/Create.cshtml");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(UsuarioCreateVM model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Roles = _context.Roles.Where(r => r.RolId == 1 || r.RolId == 2).ToList();
                ViewBag.Planes = await _context.MembresiaPlanes.Where(p => p.Activo).OrderBy(p => p.Precio).ToListAsync();
                return View("~/Views/SuperAdmin/GestionUsuarios/Create.cshtml", model);
            }

            // Email único (servidor)
            var emailExists = await _context.Usuarios
                .AsNoTracking()
                .AnyAsync(u => u.Email.ToLower() == model.Email.ToLower());
            if (emailExists)
            {
                ModelState.AddModelError(nameof(model.Email), "El email ya está registrado.");
                ViewBag.Roles = _context.Roles.Where(r => r.RolId == 1 || r.RolId == 2).ToList();
                ViewBag.Planes = await _context.MembresiaPlanes.Where(p => p.Activo).OrderBy(p => p.Precio).ToListAsync();
                return View("~/Views/SuperAdmin/GestionUsuarios/Create.cshtml", model);
            }

            // Teléfono único (servidor)
            var telefonoExists = await _context.Usuarios
                .AsNoTracking()
                .AnyAsync(u => u.Telefono == model.Telefono);
            if (telefonoExists)
            {
                ModelState.AddModelError(nameof(model.Telefono), "Este número de teléfono ya está registrado.");
                ViewBag.Roles = _context.Roles.Where(r => r.RolId == 1 || r.RolId == 2).ToList();
                ViewBag.Planes = await _context.MembresiaPlanes.Where(p => p.Activo).OrderBy(p => p.Precio).ToListAsync();
                return View("~/Views/SuperAdmin/GestionUsuarios/Create.cshtml", model);
            }

            // 👇 NUEVO: Determinar si la cuenta estará activa según la membresía
            bool activarCuenta = model.MembresiaPlanId.HasValue;

            var usuario = new Usuario
            {
                Nombre = model.Nombre,
                Email = model.Email,
                Telefono = model.Telefono,
                FechaRegistro = DateTime.Now,
                RolId = model.RolId,
                Activo = activarCuenta // 👈 Se activa solo si tiene membresía
            };

            usuario.Password = _hasher.HashPassword(usuario, model.Password);

            _context.Usuarios.Add(usuario);
            await _context.SaveChangesAsync();

            // 👇 NUEVO: Si seleccionó membresía, crearla
            if (model.MembresiaPlanId.HasValue)
            {
                var plan = await _context.MembresiaPlanes.FindAsync(model.MembresiaPlanId.Value);

                if (plan != null)
                {
                    var now = DateTime.UtcNow;
                    var membresia = new MembresiaUsuario
                    {
                        UsuarioId = usuario.UsuarioId,
                        MembresiaPlanId = plan.MembresiaPlanId,
                        Precio = plan.Precio,
                        FechaInicio = now,
                        FechaFin = now.AddDays(plan.DuracionDias),
                        Activa = true
                    };

                    _context.MembresiasUsuarios.Add(membresia);
                    await _context.SaveChangesAsync();

                    TempData["Success"] = $"Usuario {usuario.Nombre} creado exitosamente con membresía {plan.Nombre}. Cuenta activada.";
                }
                else
                {
                    TempData["Warning"] = $"Usuario {usuario.Nombre} creado, pero el plan de membresía no fue encontrado. Cuenta activada sin membresía.";
                }
            }
            else
            {
                TempData["Info"] = $"Usuario {usuario.Nombre} creado exitosamente. Cuenta inactiva - deberá comprar una membresía para activarla.";
            }

            return RedirectToAction(nameof(Index));
        }

        // ... resto de métodos (Edit, Delete, Toggle, etc.) sin cambios

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

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(UsuarioEditVM model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Roles = _context.Roles.Where(r => r.RolId == 1 || r.RolId == 2).ToList();
                return View("~/Views/SuperAdmin/GestionUsuarios/Edit.cshtml", model);
            }

            var emailTaken = await _context.Usuarios
                .AsNoTracking()
                .AnyAsync(u => u.Email.ToLower() == model.Email.ToLower() && u.UsuarioId != model.UsuarioId);
            if (emailTaken)
            {
                ModelState.AddModelError(nameof(model.Email), "El email ya está registrado.");
                ViewBag.Roles = _context.Roles.Where(r => r.RolId == 1 || r.RolId == 2).ToList();
                return View("~/Views/SuperAdmin/GestionUsuarios/Edit.cshtml", model);
            }

            var telefonoTaken = await _context.Usuarios
                .AsNoTracking()
                .AnyAsync(u => u.Telefono == model.Telefono && u.UsuarioId != model.UsuarioId);
            if (telefonoTaken)
            {
                ModelState.AddModelError(nameof(model.Telefono), "Este número de teléfono ya está registrado.");
                ViewBag.Roles = _context.Roles.Where(r => r.RolId == 1 || r.RolId == 2).ToList();
                return View("~/Views/SuperAdmin/GestionUsuarios/Edit.cshtml", model);
            }

            var usuario = await _context.Usuarios.FindAsync(model.UsuarioId);
            if (usuario == null) return NotFound();

            // 👇 NUEVO: Detectar ascenso de Cliente (1) a Gymbro (2)
            bool fueAscendido = false;
            if (usuario.RolId == 1 && model.RolId == 2)
            {
                fueAscendido = true;
                usuario.MostrarMensajeAscenso = true;
                usuario.FechaAscenso = DateTime.UtcNow;
            }

            usuario.Nombre = model.Nombre;
            usuario.Email = model.Email;
            usuario.Telefono = model.Telefono;
            usuario.RolId = model.RolId;
            usuario.Activo = model.Activo;

            if (!string.IsNullOrWhiteSpace(model.NewPassword))
                usuario.Password = _hasher.HashPassword(usuario, model.NewPassword);

            _context.Usuarios.Update(usuario);
            await _context.SaveChangesAsync();

            // 👇 NUEVO: Mensaje diferente si fue ascendido
            if (fueAscendido)
            {
                TempData["Success"] = $"¡{usuario.Nombre} ha sido ascendido a Gymbro! Recibirá un mensaje de felicitación en su próximo inicio de sesión.";
            }
            else
            {
                TempData["Success"] = "Usuario actualizado exitosamente.";
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EliminarPermanente(int id)
        {
            var usuario = await _context.Usuarios
                .Include(u => u.Rol)
                .FirstOrDefaultAsync(u => u.UsuarioId == id);

            if (usuario == null)
            {
                TempData["Error"] = "Usuario no encontrado.";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                var membresias = await _context.MembresiasUsuarios.Where(m => m.UsuarioId == id).ToListAsync();
                _context.MembresiasUsuarios.RemoveRange(membresias);

                var cartItems = await _context.CartItems.Where(c => c.UsuarioId == id).ToListAsync();
                _context.CartItems.RemoveRange(cartItems);

                var rutinas = await _context.Rutinas.Where(r => r.ClienteId == id || r.EmpleadoId == id).ToListAsync();
                foreach (var rutina in rutinas)
                {
                    if (rutina.ClienteId == id) rutina.ClienteId = null;
                    if (rutina.EmpleadoId == id) rutina.EmpleadoId = null;
                }

                var planesAlimenticios = await _context.PlanesAlimenticios.Where(p => p.ClienteId == id || p.EmpleadoId == id).ToListAsync();
                foreach (var plan in planesAlimenticios)
                {
                    if (plan.ClienteId == id) plan.ClienteId = null;
                    if (plan.EmpleadoId == id) plan.EmpleadoId = null;
                }

                var ventas = await _context.Ventas.Where(v => v.ClienteId == id || v.EmpleadoId == id).ToListAsync();
                foreach (var venta in ventas)
                {
                    if (venta.ClienteId == id) venta.ClienteId = null;
                    if (venta.EmpleadoId == id) venta.EmpleadoId = null;
                }

                _context.Usuarios.Remove(usuario);
                await _context.SaveChangesAsync();

                TempData["Success"] = $"Usuario {usuario.Nombre} eliminado permanentemente del sistema.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error al eliminar el usuario: {ex.Message}";
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleActivo(int id)
        {
            var usuario = await _context.Usuarios.FindAsync(id);
            if (usuario == null) return NotFound();

            if (usuario.Activo)
            {
                var membresiasActivas = await _context.MembresiasUsuarios
                    .Where(m => m.UsuarioId == id && m.Activa)
                    .ToListAsync();

                foreach (var membresia in membresiasActivas)
                {
                    membresia.Activa = false;
                }

                TempData["Success"] = $"Cuenta de {usuario.Nombre} desactivada. Deberá adquirir una nueva membresía para reactivarla.";
            }
            else
            {
                TempData["Info"] = $"Cuenta de {usuario.Nombre} activada. Sin embargo, necesitará una membresía activa para usar el sistema.";
            }

            usuario.Activo = !usuario.Activo;
            _context.Usuarios.Update(usuario);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }
    }
}