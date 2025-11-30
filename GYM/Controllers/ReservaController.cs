using GYM.Data;
using GYM.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
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
        /// Vista principal de reservas con horario fijo
        /// </summary>
        public async Task<IActionResult> Index()
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

            // Obtener reservas del usuario con asistencia
            var reservas = await _context.Reservas
                .Include(r => r.MarcadoPor)
                .Where(r => r.UsuarioId == userId)
                .OrderByDescending(r => r.FechaReserva)
                .ThenByDescending(r => r.HoraInicio)
                .ToListAsync();

            // Obtener horarios fijos del usuario
            var horariosFijos = await _context.HorariosFijos
                .Where(h => h.UsuarioId == userId && h.Activo)
                .OrderBy(h => h.DiaSemana)
                .ThenBy(h => h.HoraInicio)
                .ToListAsync();

            // Estadísticas de asistencia
            var totalReservas = reservas.Count(r => r.Estado != EstadoReserva.Cancelada);
            var asistencias = reservas.Count(r => r.Asistio == true);
            var faltas = reservas.Count(r => r.Asistio == false);
            var pendientes = reservas.Count(r => r.Asistio == null && r.Estado != EstadoReserva.Cancelada);

            ViewData["TotalReservas"] = totalReservas;
            ViewData["Asistencias"] = asistencias;
            ViewData["Faltas"] = faltas;
            ViewData["Pendientes"] = pendientes;
            ViewData["HorariosFijos"] = horariosFijos;

            return View(reservas);
        }

        /// <summary>
        /// Vista del calendario de reservas
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Calendario()
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

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
                HoraInicio = !string.IsNullOrEmpty(hora) ? TimeSpan.Parse(hora) : new TimeSpan(8, 0, 0),
                DuracionHoras = 2 // Valor por defecto
            };

            model.HoraFin = model.HoraInicio.Add(TimeSpan.FromHours(model.DuracionHoras));

            return View(model);
        }

        /// <summary>
        /// Crear nueva reserva con validación de conflictos
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Crear(Reserva model)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            // Validaciones básicas
            if (model.FechaReserva.Date < DateTime.Today)
            {
                ModelState.AddModelError("FechaReserva", "No puedes reservar en fechas pasadas.");
            }

            if (model.FechaReserva.DayOfWeek == DayOfWeek.Sunday)
            {
                ModelState.AddModelError("FechaReserva", "El gimnasio no abre los domingos.");
            }

            if (model.HoraInicio < new TimeSpan(8, 0, 0) || model.HoraInicio >= new TimeSpan(23, 0, 0))
            {
                ModelState.AddModelError("HoraInicio", "El horario debe ser entre 8:00 AM y 11:00 PM.");
            }

            // Validar duración
            if (model.DuracionHoras < 1 || model.DuracionHoras > 4)
            {
                model.DuracionHoras = 2;
            }

            model.HoraFin = model.HoraInicio.Add(TimeSpan.FromHours(model.DuracionHoras));

            if (model.HoraFin > new TimeSpan(23, 0, 0))
            {
                ModelState.AddModelError("DuracionHoras", $"La sesión terminaría después de las 11:00 PM. Máximo {(23 - model.HoraInicio.Hours)} horas desde esta hora.");
            }

            // ✅ VALIDACIÓN 1: Verificar conflictos con otras RESERVAS del mismo usuario
            var conflictoReservas = await _context.Reservas
                .AnyAsync(r => r.UsuarioId == userId &&
                              r.FechaReserva.Date == model.FechaReserva.Date &&
                              r.Estado != EstadoReserva.Cancelada &&
                              ((model.HoraInicio >= r.HoraInicio && model.HoraInicio < r.HoraFin) ||
                               (model.HoraFin > r.HoraInicio && model.HoraFin <= r.HoraFin) ||
                               (model.HoraInicio <= r.HoraInicio && model.HoraFin >= r.HoraFin)));

            if (conflictoReservas)
            {
                ModelState.AddModelError("", "Ya tienes una reserva en este horario.");
            }

            // ✅ VALIDACIÓN 2: Verificar conflictos con HORARIO FIJO del mismo usuario
            var diaSemanaReserva = model.FechaReserva.DayOfWeek;
            var conflictoHorarioFijo = await _context.HorariosFijos
                .AnyAsync(h => h.UsuarioId == userId &&
                              h.Activo &&
                              h.DiaSemana == diaSemanaReserva &&
                              ((model.HoraInicio >= h.HoraInicio && model.HoraInicio < h.HoraFin) ||
                               (model.HoraFin > h.HoraInicio && model.HoraFin <= h.HoraFin) ||
                               (model.HoraInicio <= h.HoraInicio && model.HoraFin >= h.HoraFin)));

            if (conflictoHorarioFijo)
            {
                ModelState.AddModelError("", "Esta reserva coincide con tu horario fijo semanal. Verifica tu rutina de entrenamiento regular.");
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            model.UsuarioId = userId;
            model.Estado = EstadoReserva.Confirmada;
            model.FechaCreacion = DateTime.Now;
            model.Asistio = null;

            _context.Reservas.Add(model);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Reserva creada exitosamente.";
            return RedirectToAction(nameof(Index));
        }

        /// <summary>
        /// Crear horario fijo con validación de conflictos
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CrearHorarioFijo(HorarioFijo model)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            // Validaciones básicas
            if (model.DiaSemana == DayOfWeek.Sunday)
            {
                TempData["Error"] = "El gimnasio no abre los domingos.";
                return RedirectToAction(nameof(Index));
            }

            if (model.HoraInicio >= model.HoraFin)
            {
                TempData["Error"] = "La hora de inicio debe ser menor a la hora de fin.";
                return RedirectToAction(nameof(Index));
            }

            if (model.HoraInicio < new TimeSpan(8, 0, 0) || model.HoraFin > new TimeSpan(23, 0, 0))
            {
                TempData["Error"] = "El horario debe ser entre 8:00 AM y 11:00 PM.";
                return RedirectToAction(nameof(Index));
            }

            // ✅ VALIDACIÓN 1: Verificar conflictos con otros HORARIOS FIJOS del mismo usuario
            var conflictoHorariosFijos = await _context.HorariosFijos
                .AnyAsync(h => h.UsuarioId == userId &&
                              h.Activo &&
                              h.DiaSemana == model.DiaSemana &&
                              ((model.HoraInicio >= h.HoraInicio && model.HoraInicio < h.HoraFin) ||
                               (model.HoraFin > h.HoraInicio && model.HoraFin <= h.HoraFin) ||
                               (model.HoraInicio <= h.HoraInicio && model.HoraFin >= h.HoraFin)));

            if (conflictoHorariosFijos)
            {
                TempData["Error"] = "Ya tienes un horario fijo en ese día y hora.";
                return RedirectToAction(nameof(Index));
            }

            // ✅ VALIDACIÓN 2: Verificar conflictos con RESERVAS FUTURAS del mismo usuario
            // SOLUCIÓN: Primero cargar todas las reservas futuras, luego filtrar en memoria por DayOfWeek
            var reservasFuturas = await _context.Reservas
                .Where(r => r.UsuarioId == userId &&
                           r.FechaReserva.Date >= DateTime.Today &&
                           r.Estado != EstadoReserva.Cancelada)
                .ToListAsync(); // ✅ Cargar en memoria primero

            // ✅ Filtrar en memoria por día de la semana
            var reservasDelDia = reservasFuturas
                .Where(r => r.FechaReserva.DayOfWeek == model.DiaSemana)
                .ToList();

            // ✅ Verificar conflictos de horario
            var reservasEnConflicto = reservasDelDia
                .Where(r => (model.HoraInicio >= r.HoraInicio && model.HoraInicio < r.HoraFin) ||
                           (model.HoraFin > r.HoraInicio && model.HoraFin <= r.HoraFin) ||
                           (model.HoraInicio <= r.HoraInicio && model.HoraFin >= r.HoraFin))
                .ToList();

            if (reservasEnConflicto.Any())
            {
                var primeraReserva = reservasEnConflicto.First();
                TempData["Error"] = $"Este horario fijo entra en conflicto con tu reserva del {primeraReserva.FechaReserva:dd/MM/yyyy} a las {primeraReserva.HoraInicio:hh\\:mm}. Cancela primero las reservas en conflicto.";
                return RedirectToAction(nameof(Index));
            }

            model.UsuarioId = userId;
            model.Activo = true;
            model.FechaCreacion = DateTime.Now;

            _context.HorariosFijos.Add(model);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Horario fijo agregado exitosamente.";
            return RedirectToAction(nameof(Index));
        }

        /// <summary>
        /// Eliminar horario fijo
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EliminarHorarioFijo(int id)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            var horario = await _context.HorariosFijos
                .FirstOrDefaultAsync(h => h.HorarioFijoId == id && h.UsuarioId == userId);

            if (horario == null)
            {
                TempData["Error"] = "Horario no encontrado.";
                return RedirectToAction(nameof(Index));
            }

            _context.HorariosFijos.Remove(horario);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Horario fijo eliminado exitosamente.";
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
        /// Historial de asistencia
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> MiAsistencia(int? mes, int? anio)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            var mesSeleccionado = mes ?? DateTime.Now.Month;
            var anioSeleccionado = anio ?? DateTime.Now.Year;

            var reservas = await _context.Reservas
                .Include(r => r.MarcadoPor)
                .Where(r => r.UsuarioId == userId &&
                           r.FechaReserva.Month == mesSeleccionado &&
                           r.FechaReserva.Year == anioSeleccionado &&
                           r.Estado != EstadoReserva.Cancelada)
                .OrderBy(r => r.FechaReserva)
                .ThenBy(r => r.HoraInicio)
                .ToListAsync();

            var totalDias = reservas.Select(r => r.FechaReserva.Date).Distinct().Count();
            var asistencias = reservas.Count(r => r.Asistio == true);
            var faltas = reservas.Count(r => r.Asistio == false);
            var pendientes = reservas.Count(r => r.Asistio == null);

            ViewData["MesSeleccionado"] = mesSeleccionado;
            ViewData["AnioSeleccionado"] = anioSeleccionado;
            ViewData["TotalDias"] = totalDias;
            ViewData["Asistencias"] = asistencias;
            ViewData["Faltas"] = faltas;
            ViewData["Pendientes"] = pendientes;
            ViewData["PorcentajeAsistencia"] = (asistencias + faltas) > 0
                ? (int)((asistencias * 100.0) / (asistencias + faltas))
                : 0;

            return View(reservas);
        }

        /// <summary>
        /// API: Obtener disponibilidad con advertencias de conflicto con horario fijo
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> ObtenerDisponibilidad(DateTime fecha)
        {
            if (fecha.DayOfWeek == DayOfWeek.Sunday)
            {
                return Json(new { success = false, message = "El gimnasio no abre los domingos." });
            }

            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var diaSemana = fecha.DayOfWeek;

            // Obtener horarios fijos del usuario para ese día de la semana
            var horariosFixosDelDia = await _context.HorariosFijos
                .Where(h => h.UsuarioId == userId &&
                           h.Activo &&
                           h.DiaSemana == diaSemana)
                .ToListAsync();

            var horarios = new List<object>();
            for (int hora = 8; hora < 23; hora++)
            {
                var horaInicio = new TimeSpan(hora, 0, 0);
                var horaFin = new TimeSpan(hora + 1, 0, 0);

                var reservasEnHorario = await _context.Reservas
                    .CountAsync(r => r.FechaReserva.Date == fecha.Date &&
                                    r.Estado != EstadoReserva.Cancelada &&
                                    r.HoraInicio < horaFin &&
                                    r.HoraFin > horaInicio);

                // ✅ Verificar si este horario coincide con algún horario fijo del usuario
                var conflictoHorarioFijo = horariosFixosDelDia
                    .Any(h => (horaInicio >= h.HoraInicio && horaInicio < h.HoraFin) ||
                             (horaFin > h.HoraInicio && horaFin <= h.HoraFin) ||
                             (horaInicio <= h.HoraInicio && horaFin >= h.HoraFin));

                var horarioFijoConflicto = horariosFixosDelDia
                    .FirstOrDefault(h => (horaInicio >= h.HoraInicio && horaInicio < h.HoraFin) ||
                                        (horaFin > h.HoraInicio && horaFin <= h.HoraFin) ||
                                        (horaInicio <= h.HoraInicio && horaFin >= h.HoraFin));

                horarios.Add(new
                {
                    hora = hora,
                    horaTexto = $"{hora:D2}:00 - {(hora + 1):D2}:00",
                    disponible = reservasEnHorario < 20,
                    ocupadas = reservasEnHorario,
                    capacidad = 20,
                    conflictoHorarioFijo = conflictoHorarioFijo, // ✅ Nueva propiedad
                    tipoEntrenamientoFijo = horarioFijoConflicto?.TipoEntrenamiento, // ✅ Nueva propiedad
                    mensajeConflicto = conflictoHorarioFijo
                        ? $"⚠️ Coincide con tu horario fijo de {horarioFijoConflicto?.TipoEntrenamiento}"
                        : null // ✅ Nueva propiedad
                });
            }

            return Json(new { success = true, horarios });
        }
        // Agregar este método al final de ReservasController
        /// <summary>
        /// ✅ SIMPLIFICADO: Calendario semanal del cliente SIN JSON
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> CalendarioSemanal()
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            // Verificar membresía activa
            var now = DateTime.UtcNow;
            var tieneMembresia = await _context.MembresiasUsuarios
                .AnyAsync(m => m.UsuarioId == userId && m.Activa &&
                              m.FechaInicio <= now && m.FechaFin >= now);

            if (!tieneMembresia)
            {
                TempData["Error"] = "Necesitas una membresía activa para ver el calendario.";
                return RedirectToAction("Index", "MiMembresia");
            }

            // Obtener horarios fijos del cliente
            var horariosFijos = await _context.HorariosFijos
                .Where(h => h.UsuarioId == userId && h.Activo)
                .ToListAsync();

            // Obtener reservas futuras de la próxima semana
            var inicioSemana = DateTime.Today;
            var finSemana = inicioSemana.AddDays(7);

            var reservasFuturas = await _context.Reservas
                .Where(r => r.UsuarioId == userId &&
                           r.FechaReserva >= inicioSemana &&
                           r.FechaReserva < finSemana &&
                           r.Estado != EstadoReserva.Cancelada)
                .ToListAsync();

            // ✅ Obtener rutinas activas del cliente CON ejercicios
            var rutinas = await _context.Rutinas
                .Include(r => r.Empleado)
                .Include(r => r.Ejercicios)
                .Where(r => r.ClienteId == userId && r.Activa)
                .ToListAsync();

            ViewData["HorariosFijos"] = horariosFijos;
            ViewData["ReservasFuturas"] = reservasFuturas;
            ViewData["Rutinas"] = rutinas;
            ViewData["InicioSemana"] = inicioSemana;

            return View("~/Views/Reservas/CalendarioSemanal.cshtml");
        }
    }
}