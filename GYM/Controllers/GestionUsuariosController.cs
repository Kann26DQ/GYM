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

        /// <summary>
        /// ✅ NUEVO: Index con filtro de búsqueda
        /// </summary>
        public async Task<IActionResult> Index(string buscar, string tipoFiltro = "nombre")
        {
            var query = _context.Usuarios
                .Include(u => u.Rol)
                .Where(u => (u.RolId == 1 || u.RolId == 2))
                .AsQueryable();

            // ✅ Aplicar filtros de búsqueda
            if (!string.IsNullOrWhiteSpace(buscar))
            {
                buscar = buscar.Trim();

                switch (tipoFiltro.ToLower())
                {
                    case "nombre":
                        query = query.Where(u => u.Nombre.ToLower().Contains(buscar.ToLower()));
                        break;
                    case "email":
                        query = query.Where(u => u.Email.ToLower().Contains(buscar.ToLower()));
                        break;
                    case "telefono":
                        query = query.Where(u => u.Telefono.Contains(buscar));
                        break;
                    case "rol":
                        query = query.Where(u => u.Rol.Nombre.ToLower().Contains(buscar.ToLower()));
                        break;
                    default:
                        // Búsqueda general en todos los campos
                        query = query.Where(u =>
                            u.Nombre.ToLower().Contains(buscar.ToLower()) ||
                            u.Email.ToLower().Contains(buscar.ToLower()) ||
                            u.Telefono.Contains(buscar) ||
                            u.Rol.Nombre.ToLower().Contains(buscar.ToLower()));
                        break;
                }
            }

            var usuarios = await query
                .OrderBy(u => u.RolId)
                .ThenBy(u => u.Nombre)
                .ToListAsync();

            // Pasar valores al ViewBag para mantenerlos en la vista
            ViewBag.BuscarActual = buscar;
            ViewBag.TipoFiltroActual = tipoFiltro;

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

            bool activarCuenta = model.MembresiaPlanId.HasValue;

            var usuario = new Usuario
            {
                Nombre = model.Nombre,
                Email = model.Email,
                Telefono = model.Telefono,
                FechaRegistro = DateTime.Now,
                RolId = model.RolId,
                Activo = activarCuenta
            };

            usuario.Password = _hasher.HashPassword(usuario, model.Password);

            _context.Usuarios.Add(usuario);
            await _context.SaveChangesAsync();

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
                // ✅ Eliminar membresías
                var membresias = await _context.MembresiasUsuarios
                    .Where(m => m.UsuarioId == id)
                    .ToListAsync();
                _context.MembresiasUsuarios.RemoveRange(membresias);

                // ✅ Eliminar items del carrito
                var cartItems = await _context.CartItems
                    .Where(c => c.UsuarioId == id)
                    .ToListAsync();
                _context.CartItems.RemoveRange(cartItems);

                // ✅ Eliminar reservas del usuario
                var reservas = await _context.Reservas
                    .Where(r => r.UsuarioId == id)
                    .ToListAsync();
                _context.Reservas.RemoveRange(reservas);

                // ✅ Eliminar horarios fijos
                var horariosFijos = await _context.HorariosFijos
                    .Where(h => h.UsuarioId == id)
                    .ToListAsync();
                _context.HorariosFijos.RemoveRange(horariosFijos);

                // ✅ Eliminar evaluaciones de rendimiento
                var evaluaciones = await _context.EvaluacionesRendimiento
                    .Where(e => e.ClienteId == id || e.EmpleadoId == id)
                    .ToListAsync();
                _context.EvaluacionesRendimiento.RemoveRange(evaluaciones);

                // ✅ Actualizar rutinas (SET NULL en claves foráneas)
                var rutinasCliente = await _context.Rutinas
                    .Where(r => r.ClienteId == id)
                    .ToListAsync();
                foreach (var rutina in rutinasCliente)
                {
                    rutina.ClienteId = 0; // O podrías eliminarlas: _context.Rutinas.Remove(rutina);
                }

                var rutinasEmpleado = await _context.Rutinas
                    .Where(r => r.EmpleadoId == id)
                    .ToListAsync();
                foreach (var rutina in rutinasEmpleado)
                {
                    rutina.EmpleadoId = 0;
                }

                // ✅ Actualizar planes alimenticios
                var planesCliente = await _context.PlanesAlimenticios
                    .Where(p => p.ClienteId == id)
                    .ToListAsync();
                foreach (var plan in planesCliente)
                {
                    plan.ClienteId = 0;
                }

                var planesEmpleado = await _context.PlanesAlimenticios
                    .Where(p => p.EmpleadoId == id)
                    .ToListAsync();
                foreach (var plan in planesEmpleado)
                {
                    plan.EmpleadoId = 0;
                }

                // ✅ Actualizar ventas
                var ventasCliente = await _context.Ventas
                    .Where(v => v.ClienteId == id)
                    .ToListAsync();
                foreach (var venta in ventasCliente)
                {
                    venta.ClienteId = 0;
                }

                var ventasEmpleado = await _context.Ventas
                    .Where(v => v.EmpleadoId == id)
                    .ToListAsync();
                foreach (var venta in ventasEmpleado)
                {
                    venta.EmpleadoId = 0;
                }

                // ✅ Actualizar movimientos de stock
                var movimientos = await _context.MovimientosStock
                    .Where(m => m.UsuarioId == id)
                    .ToListAsync();
                foreach (var movimiento in movimientos)
                {
                    movimiento.UsuarioId = 0;
                }

                // ✅ Actualizar reportes
                var reportes = await _context.Reportes
                    .Where(r => r.EmpleadoId == id)
                    .ToListAsync();
                foreach (var reporte in reportes)
                {
                    reporte.EmpleadoId = 0;
                }

                // ✅ FINALMENTE eliminar el usuario
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