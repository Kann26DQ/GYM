using GYM.Data;
using GYM.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace GYM.Controllers
{
    [Authorize(Roles = "Cliente,Gymbro")]
    public class ReservasController : Controller
    {
        private readonly AppDBContext _context;

        public ReservasController(AppDBContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Vista principal de reservas
        /// </summary>
        public async Task<IActionResult> Index()
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            // Verificar que el usuario tenga membresía activa
            var now = DateTime.UtcNow;
            var tieneMembresia = await _context.MembresiasUsuarios
                .AnyAsync(m => m.UsuarioId == userId && m.Activa &&
                              m.FechaInicio <= now && m.FechaFin >= now);

            if (!tieneMembresia)
            {
                TempData["Error"] = "Necesitas una membresía activa para reservar sesiones.";
                return RedirectToAction("Index", "MiMembresia");
            }

            // Obtener reservas del usuario
            var reservas = await _context.Reservas
                .Where(r => r.UsuarioId == userId)
                .OrderByDescending(r => r.FechaReserva)
                .ThenByDescending(r => r.HoraInicio)
                .ToListAsync();

            return View(reservas);
        }

        /// <summary>
        /// Vista del calendario de reservas
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Calendario()
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            // Verificar membresía activa
            var now = DateTime.UtcNow;
            var tieneMembresia = await _context.MembresiasUsuarios
                .AnyAsync(m => m.UsuarioId == userId && m.Activa &&
                              m.FechaInicio <= now && m.FechaFin >= now);

            if (!tieneMembresia)
            {
                TempData["Error"] = "Necesitas una membresía activa para reservar sesiones.";
                return RedirectToAction("Index", "MiMembresia");
            }

            ViewData["UserId"] = userId;
            return View();
        }

        /// <summary>
        /// Formulario de nueva reserva
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Crear(string fecha, string hora)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            // Verificar membresía activa
            var now = DateTime.UtcNow;
            var tieneMembresia = await _context.MembresiasUsuarios
                .AnyAsync(m => m.UsuarioId == userId && m.Activa &&
                              m.FechaInicio <= now && m.FechaFin >= now);

            if (!tieneMembresia)
            {
                TempData["Error"] = "Necesitas una membresía activa para reservar sesiones.";
                return RedirectToAction("Index", "MiMembresia");
            }

            var model = new Reserva
            {
                FechaReserva = !string.IsNullOrEmpty(fecha) ? DateTime.Parse(fecha) : DateTime.Today.AddDays(1),
                HoraInicio = !string.IsNullOrEmpty(hora) ? TimeSpan.Parse(hora) : new TimeSpan(8, 0, 0)
            };

            // Calcular hora de fin (1 hora después)
            model.HoraFin = model.HoraInicio.Add(TimeSpan.FromHours(1));

            return View(model);
        }

        /// <summary>
        /// Crear nueva reserva
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Crear(Reserva model)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            // Validaciones
            if (model.FechaReserva.Date < DateTime.Today)
            {
                ModelState.AddModelError("FechaReserva", "No puedes reservar en fechas pasadas.");
            }

            // Validar que sea Lunes a Sábado
            if (model.FechaReserva.DayOfWeek == DayOfWeek.Sunday)
            {
                ModelState.AddModelError("FechaReserva", "El gimnasio no abre los domingos.");
            }

            // Validar horario (8 AM - 11 PM)
            if (model.HoraInicio < new TimeSpan(8, 0, 0) || model.HoraInicio >= new TimeSpan(23, 0, 0))
            {
                ModelState.AddModelError("HoraInicio", "El horario debe ser entre 8:00 AM y 11:00 PM.");
            }

            // Calcular hora de fin automáticamente
            model.HoraFin = model.HoraInicio.Add(TimeSpan.FromHours(1));

            if (model.HoraFin > new TimeSpan(23, 0, 0))
            {
                ModelState.AddModelError("HoraInicio", "La sesión terminaría después de las 11:00 PM. Selecciona un horario más temprano.");
            }

            // Validar que no tenga otra reserva a la misma hora
            var conflicto = await _context.Reservas
                .AnyAsync(r => r.UsuarioId == userId &&
                              r.FechaReserva.Date == model.FechaReserva.Date &&
                              r.Estado != EstadoReserva.Cancelada &&
                              ((model.HoraInicio >= r.HoraInicio && model.HoraInicio < r.HoraFin) ||
                               (model.HoraFin > r.HoraInicio && model.HoraFin <= r.HoraFin)));

            if (conflicto)
            {
                ModelState.AddModelError("", "Ya tienes una reserva en este horario.");
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            model.UsuarioId = userId;
            model.Estado = EstadoReserva.Confirmada;
            model.FechaCreacion = DateTime.Now;

            _context.Reservas.Add(model);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Reserva creada exitosamente.";
            return RedirectToAction(nameof(Index));
        }

        /// <summary>
        /// Cancelar reserva
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancelar(int id)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            var reserva = await _context.Reservas
                .FirstOrDefaultAsync(r => r.ReservaId == id && r.UsuarioId == userId);

            if (reserva == null)
            {
                TempData["Error"] = "Reserva no encontrada.";
                return RedirectToAction(nameof(Index));
            }

            // No permitir cancelar reservas pasadas
            var fechaHoraReserva = reserva.FechaReserva.Date + reserva.HoraInicio;
            if (fechaHoraReserva < DateTime.Now)
            {
                TempData["Error"] = "No puedes cancelar una reserva pasada.";
                return RedirectToAction(nameof(Index));
            }

            reserva.Estado = EstadoReserva.Cancelada;
            await _context.SaveChangesAsync();

            TempData["Success"] = "Reserva cancelada exitosamente.";
            return RedirectToAction(nameof(Index));
        }

        /// <summary>
        /// API: Obtener disponibilidad de horarios
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> ObtenerDisponibilidad(DateTime fecha)
        {
            // Validar que no sea domingo
            if (fecha.DayOfWeek == DayOfWeek.Sunday)
            {
                return Json(new { success = false, message = "El gimnasio no abre los domingos." });
            }

            // Generar horarios disponibles (8 AM - 11 PM, cada hora)
            var horarios = new List<object>();
            for (int hora = 8; hora < 23; hora++)
            {
                var horaInicio = new TimeSpan(hora, 0, 0);
                var horaFin = new TimeSpan(hora + 1, 0, 0);

                // Contar cuántas reservas hay en este horario (capacidad máxima: 20)
                var reservasEnHorario = await _context.Reservas
                    .CountAsync(r => r.FechaReserva.Date == fecha.Date &&
                                    r.Estado != EstadoReserva.Cancelada &&
                                    r.HoraInicio < horaFin &&
                                    r.HoraFin > horaInicio);

                horarios.Add(new
                {
                    hora = hora,
                    horaTexto = $"{hora:D2}:00 - {(hora + 1):D2}:00",
                    disponible = reservasEnHorario < 20,
                    ocupadas = reservasEnHorario,
                    capacidad = 20
                });
            }

            return Json(new { success = true, horarios });
        }
        // Agregar este método al final del controlador ReservasController

        /// <summary>
        /// Vista de historial/asistencia al gym
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Historial(int? mes, int? anio)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            // Usar mes y año actual si no se especifican
            var mesSeleccionado = mes ?? DateTime.Now.Month;
            var anioSeleccionado = anio ?? DateTime.Now.Year;

            // Obtener todas las reservas completadas/confirmadas del usuario
            var reservas = await _context.Reservas
                .Where(r => r.UsuarioId == userId &&
                           (r.Estado == EstadoReserva.Completada || r.Estado == EstadoReserva.Confirmada) &&
                           r.FechaReserva.Month == mesSeleccionado &&
                           r.FechaReserva.Year == anioSeleccionado)
                .OrderBy(r => r.FechaReserva)
                .ThenBy(r => r.HoraInicio)
                .ToListAsync();

            // Estadísticas
            var totalDias = reservas.Select(r => r.FechaReserva.Date).Distinct().Count();
            var totalHoras = reservas.Sum(r => (r.HoraFin - r.HoraInicio).TotalHours);

            ViewData["MesSeleccionado"] = mesSeleccionado;
            ViewData["AnioSeleccionado"] = anioSeleccionado;
            ViewData["TotalDias"] = totalDias;
            ViewData["TotalHoras"] = totalHoras;

            return View(reservas);
        }
    }
}