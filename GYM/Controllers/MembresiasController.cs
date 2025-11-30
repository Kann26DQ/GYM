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

        /// <summary>
        /// Mostrar la tienda de planes de membresía
        /// </summary>
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

        /// <summary>
        /// Iniciar proceso de pago para nueva membresía
        /// </summary>
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
                    TempData["Warning"] = "Ya cuentas con una membresía vigente. Ve a 'Mi Membresía' para mejorar tu plan.";
                    return RedirectToAction("Index", "MiMembresia");
                }
            }

            // Pasar datos a la vista de pago
            ViewData["Plan"] = plan;
            ViewData["Usuario"] = usuario;
            return View("~/Views/Membresias/Checkout.cshtml");
        }

        /// <summary>
        /// ✅ NUEVO: Iniciar mejora de membresía con descuento proporcional
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> IniciarMejora(int planId, bool aplicarInmediato = true)
        {
            var uidStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(uidStr, out var uid)) return Forbid();

            var usuario = await _ctx.Usuarios.FindAsync(uid);
            if (usuario == null)
            {
                TempData["Error"] = "Usuario no encontrado.";
                return RedirectToAction("Index", "MiMembresia");
            }

            // Obtener membresía actual
            var now = DateTime.UtcNow;
            var membresiaActual = await _ctx.MembresiasUsuarios
                .Include(m => m.Plan)
                .FirstOrDefaultAsync(m => m.UsuarioId == uid && m.Activa && m.FechaFin >= now);

            if (membresiaActual == null)
            {
                TempData["Error"] = "No tienes una membresía activa.";
                return RedirectToAction(nameof(Store));
            }

            // Obtener el nuevo plan
            var nuevoPlan = await _ctx.MembresiaPlanes
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.MembresiaPlanId == planId && p.Activo);

            if (nuevoPlan == null)
            {
                TempData["Error"] = "Plan no encontrado.";
                return RedirectToAction("Index", "MiMembresia");
            }

            // Validar que sea una mejora (precio mayor)
            if (nuevoPlan.Precio <= membresiaActual.Plan.Precio)
            {
                TempData["Error"] = "Solo puedes mejorar a un plan de mayor valor.";
                return RedirectToAction("Index", "MiMembresia");
            }

            // ✅ Calcular descuento proporcional por días restantes
            var diasRestantes = (membresiaActual.FechaFin - now).Days;
            var totalDias = (membresiaActual.FechaFin - membresiaActual.FechaInicio).Days;
            var descuentoProporcional = 0m;

            if (diasRestantes > 0 && totalDias > 0 && aplicarInmediato)
            {
                // Valor por día del plan actual
                var valorPorDia = membresiaActual.Precio / totalDias;
                descuentoProporcional = valorPorDia * diasRestantes;
            }

            // Calcular precio final
            var precioFinal = nuevoPlan.Precio - descuentoProporcional;

            // Asegurar que el precio final no sea negativo
            if (precioFinal < 0) precioFinal = 0;

            // Guardar info en ViewData para la vista de checkout
            ViewData["Plan"] = nuevoPlan;
            ViewData["Usuario"] = usuario;
            ViewData["EsMejora"] = true;
            ViewData["DescuentoProporcional"] = descuentoProporcional;
            ViewData["PrecioFinal"] = precioFinal;
            ViewData["DiasRestantes"] = diasRestantes;
            ViewData["MembresiaActualId"] = membresiaActual.MembresiaUsuarioId;

            return View("~/Views/Membresias/CheckoutMejora.cshtml");
        }

        /// <summary>
        /// Procesar el pago de nueva membresía
        /// </summary>
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
                return RedirectToAction("Index", "Home");
            }
            else
            {
                return RedirectToAction("Index", "Home");
            }
        }

        /// <summary>
        /// ✅ NUEVO: Procesar mejora de membresía con descuento
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ProcesarMejora(int planId, int membresiaActualId, decimal descuentoProporcional,
            string numeroTarjeta, string nombreTitular, string fechaExpiracion, string cvv)
        {
            // Validaciones básicas
            if (string.IsNullOrWhiteSpace(numeroTarjeta) || numeroTarjeta.Replace(" ", "").Length != 16)
            {
                TempData["Error"] = "Número de tarjeta inválido.";
                return RedirectToAction("Index", "MiMembresia");
            }

            if (string.IsNullOrWhiteSpace(cvv) || cvv.Length != 3)
            {
                TempData["Error"] = "CVV inválido.";
                return RedirectToAction("Index", "MiMembresia");
            }

            var uidStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(uidStr, out var uid)) return Forbid();

            // Obtener membresía actual
            var membresiaActual = await _ctx.MembresiasUsuarios
                .Include(m => m.Plan)
                .FirstOrDefaultAsync(m => m.MembresiaUsuarioId == membresiaActualId && m.UsuarioId == uid);

            if (membresiaActual == null)
            {
                TempData["Error"] = "Membresía actual no encontrada.";
                return RedirectToAction("Index", "MiMembresia");
            }

            // Obtener el nuevo plan
            var nuevoPlan = await _ctx.MembresiaPlanes
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.MembresiaPlanId == planId && p.Activo);

            if (nuevoPlan == null)
            {
                TempData["Error"] = "Plan no encontrado.";
                return RedirectToAction("Index", "MiMembresia");
            }

            // ✅ Calcular precio final con descuento
            var precioFinal = nuevoPlan.Precio - descuentoProporcional;
            if (precioFinal < 0) precioFinal = 0;

            // Simular procesamiento de pago
            var now = DateTime.UtcNow;

            // ✅ Desactivar la membresía actual
            membresiaActual.Activa = false;
            _ctx.MembresiasUsuarios.Update(membresiaActual);

            // ✅ Crear la nueva membresía (comienza AHORA, no espera)
            var nuevaMembresia = new MembresiaUsuario
            {
                UsuarioId = uid,
                MembresiaPlanId = nuevoPlan.MembresiaPlanId,
                Precio = precioFinal, // Precio con descuento aplicado
                FechaInicio = now, // ✅ Comienza inmediatamente
                FechaFin = now.AddDays(nuevoPlan.DuracionDias),
                Activa = true
            };

            _ctx.MembresiasUsuarios.Add(nuevaMembresia);
            await _ctx.SaveChangesAsync();

            TempData["Success"] = $"¡Mejora exitosa! Ahora tienes el plan {nuevoPlan.Nombre}. Ahorraste S/ {descuentoProporcional:0.00} por la mejora inmediata.";

            return RedirectToAction("Index", "MiMembresia");
        }

        /// <summary>
        /// ✅ NUEVO: Cancelar membresía activa
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelarMembresia()
        {
            var uidStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(uidStr, out var uid)) return Forbid();

            var now = DateTime.UtcNow;

            // Obtener membresía activa
            var membresiaActiva = await _ctx.MembresiasUsuarios
                .Include(m => m.Plan)
                .FirstOrDefaultAsync(m => m.UsuarioId == uid && m.Activa && m.FechaFin >= now);

            if (membresiaActiva == null)
            {
                TempData["Error"] = "No tienes una membresía activa para cancelar.";
                return RedirectToAction("Index", "MiMembresia");
            }

            // ✅ Desactivar la membresía
            membresiaActiva.Activa = false;
            membresiaActiva.FechaFin = now; // Terminar inmediatamente
            _ctx.MembresiasUsuarios.Update(membresiaActiva);

            // ✅ Opcional: Desactivar el usuario
            var usuario = await _ctx.Usuarios.FindAsync(uid);
            if (usuario != null)
            {
                usuario.Activo = false;
                _ctx.Usuarios.Update(usuario);
            }

            // ✅ Eliminar rutinas activas del cliente
            var rutinasActivas = await _ctx.Rutinas
                .Where(r => r.ClienteId == uid && r.Activa)
                .ToListAsync();

            foreach (var rutina in rutinasActivas)
            {
                rutina.Activa = false;
            }

            // ✅ CORRECCIÓN: PlanAlimenticio NO tiene propiedad "Activo"
            // Opción 1: Eliminar directamente los planes
            var planesActivos = await _ctx.PlanesAlimenticios
                .Where(p => p.ClienteId == uid)
                .ToListAsync();

            // Simplemente los eliminamos de la base de datos
            _ctx.PlanesAlimenticios.RemoveRange(planesActivos);

            await _ctx.SaveChangesAsync();

            TempData["Success"] = "Tu membresía ha sido cancelada exitosamente. Esperamos verte pronto de nuevo.";

            // Redirigir al home (ahora aparecerá como sin membresía)
            return RedirectToAction("Index", "Home");
        }
    }
}