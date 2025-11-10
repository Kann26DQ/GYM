using GYM.Data;
using GYM.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace GYM.Controllers
{
    [Authorize]
    public class MembresiasController : Controller
    {
        private readonly AppDBContext _ctx;
        public MembresiasController(AppDBContext ctx) => _ctx = ctx;

        [HttpGet]
        public async Task<IActionResult> Store(string tipo = "mensual")
        {
            // Filtrar membresías según tipo
            IQueryable<MembresiaPlan> query = _ctx.MembresiaPlanes
                .AsNoTracking()
                .Where(p => p.Activo);

            if (tipo == "anual")
            {
                // Planes anuales: duración entre 300 y 365 días
                query = query.Where(p => p.DuracionDias >= 300 && p.DuracionDias <= 365);
            }
            else
            {
                // Planes mensuales: duración menor a 100 días
                query = query.Where(p => p.DuracionDias < 100);
            }

            var planes = await query
                .OrderBy(p => p.Precio)
                .Take(4) // Mostrar hasta 4 planes
                .ToListAsync();

            ViewData["TipoMembresia"] = tipo;
            return View("~/Views/Membresias/Store.cshtml", planes);
        }

        // Nueva acción: Mostrar formulario de pago
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> IniciarPago(int id)
        {
            var plan = await _ctx.MembresiaPlanes
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.MembresiaPlanId == id && p.Activo);

            if (plan == null)
            {
                TempData["Error"] = "Plan inválido.";
                return RedirectToAction(nameof(Store));
            }

            var uidStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(uidStr, out var uid)) return Forbid();

            var usuario = await _ctx.Usuarios.FindAsync(uid);
            if (usuario == null)
            {
                TempData["Error"] = "Usuario no encontrado.";
                return RedirectToAction(nameof(Store));
            }

            // Solo verificar membresía activa si el usuario ya está activo
            if (usuario.Activo)
            {
                var now = DateTime.UtcNow;
                var yaActiva = await _ctx.MembresiasUsuarios
                    .AnyAsync(m => m.UsuarioId == uid && m.Activa && m.FechaInicio <= now && m.FechaFin >= now);

                if (yaActiva)
                {
                    TempData["Warning"] = "Ya cuentas con una membresía vigente.";
                    return RedirectToAction(nameof(Store));
                }
            }

            // Pasar datos a la vista de pago
            ViewData["Plan"] = plan;
            ViewData["Usuario"] = usuario;
            return View("~/Views/Membresias/Checkout.cshtml");
        }

        // Procesar el pago
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ProcesarPago(int planId, string numeroTarjeta, string nombreTitular,
            string fechaExpiracion, string cvv)
        {
            // Validaciones básicas
            if (string.IsNullOrWhiteSpace(numeroTarjeta) || numeroTarjeta.Replace(" ", "").Length != 16)
            {
                TempData["Error"] = "Número de tarjeta inválido.";
                var planForError = await _ctx.MembresiaPlanes.FindAsync(planId);
                if (planForError != null)
                {
                    var usuarioForError = await _ctx.Usuarios.FindAsync(int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!));
                    ViewData["Plan"] = planForError;
                    ViewData["Usuario"] = usuarioForError;
                    return View("~/Views/Membresias/Checkout.cshtml");
                }
                return RedirectToAction(nameof(Store));
            }

            if (string.IsNullOrWhiteSpace(cvv) || cvv.Length != 3)
            {
                TempData["Error"] = "CVV inválido.";
                var planForError = await _ctx.MembresiaPlanes.FindAsync(planId);
                if (planForError != null)
                {
                    var usuarioForError = await _ctx.Usuarios.FindAsync(int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!));
                    ViewData["Plan"] = planForError;
                    ViewData["Usuario"] = usuarioForError;
                    return View("~/Views/Membresias/Checkout.cshtml");
                }
                return RedirectToAction(nameof(Store));
            }

            var plan = await _ctx.MembresiaPlanes
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.MembresiaPlanId == planId && p.Activo);

            if (plan == null)
            {
                TempData["Error"] = "Plan no encontrado.";
                return RedirectToAction(nameof(Store));
            }

            var uidStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(uidStr, out var uid)) return Forbid();

            var usuario = await _ctx.Usuarios.FindAsync(uid);
            if (usuario == null)
            {
                TempData["Error"] = "Usuario no encontrado.";
                return RedirectToAction(nameof(Store));
            }

            // Simular procesamiento de pago
            var now = DateTime.UtcNow;

            // Desactivar membresías anteriores si existen
            var membresiasAnteriores = await _ctx.MembresiasUsuarios
                .Where(m => m.UsuarioId == uid && m.Activa)
                .ToListAsync();

            foreach (var mem in membresiasAnteriores)
            {
                mem.Activa = false;
            }

            // Crear la nueva membresía
            var mu = new MembresiaUsuario
            {
                UsuarioId = uid,
                MembresiaPlanId = plan.MembresiaPlanId,
                Precio = plan.Precio,
                FechaInicio = now,
                FechaFin = now.AddDays(plan.DuracionDias),
                Activa = true
            };

            _ctx.MembresiasUsuarios.Add(mu);

            // Activar la cuenta del usuario si estaba inactiva
            if (!usuario.Activo)
            {
                usuario.Activo = true;
                _ctx.Usuarios.Update(usuario);
            }

            await _ctx.SaveChangesAsync();

            TempData["Success"] = $"¡Pago procesado exitosamente! Has adquirido la membresía {plan.Nombre}.";

            // Redirigir según el rol
            if (User.IsInRole("Gymbro"))
            {
                return RedirectToAction("Index", "gymbro");
            }
            else if (User.IsInRole("Cliente"))
            {
                return RedirectToAction("Index", "Home");
            }
            else
            {
                return RedirectToAction("Index", "Home");
            }
        }
    }
}