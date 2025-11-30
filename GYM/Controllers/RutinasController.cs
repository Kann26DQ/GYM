using GYM.Data;
using GYM.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace GYM.Controllers
{
    // ? Cliente y Gymbro: Ver sus rutinas
    [Authorize(Roles = "Cliente,Gymbro")]
    public class RutinasController : Controller
    {
        private readonly AppDBContext _context;

        public RutinasController(AppDBContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Vista de rutinas del cliente
        /// </summary>
        public async Task<IActionResult> Index()
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            var rutinas = await _context.Rutinas
                .Include(r => r.Empleado)
                .Include(r => r.EvaluacionBase)
                .Include(r => r.Ejercicios)
                .Where(r => r.ClienteId == userId && r.Activa)
                .OrderByDescending(r => r.FechaCreacion)
                .ToListAsync();

            return View(rutinas);
        }

        /// <summary>
        /// Ver detalles de una rutina
        /// </summary>
        public async Task<IActionResult> Detalles(int id)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            var rutina = await _context.Rutinas
                .Include(r => r.Empleado)
                .Include(r => r.EvaluacionBase)
                .Include(r => r.Ejercicios)
                .FirstOrDefaultAsync(r => r.RutinaId == id && r.ClienteId == userId);

            if (rutina == null)
            {
                TempData["Error"] = "Rutina no encontrada.";
                return RedirectToAction(nameof(Index));
            }

            return View(rutina);
        }
    }

    // ? SOLO Gymbro (empleado) puede gestionar rutinas
    [Authorize(Roles = "Gymbro")]
    public class GestionRutinasController : Controller
    {
        private readonly AppDBContext _context;

        public GestionRutinasController(AppDBContext context)
        {
            _context = context;
        }

        /// <summary>
        /// ? CORREGIDO: Index - Mostrar SOLO clientes SIN evaluación (disponibles)
        /// </summary>
        public async Task<IActionResult> Index()
        {
            var empleadoId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            var usuario = await _context.Usuarios
                .Include(u => u.Rol)
                .FirstOrDefaultAsync(u => u.UsuarioId == empleadoId);

            if (usuario == null || usuario.RolId != 2)
            {
                TempData["Error"] = "Solo los empleados (Gymbro) pueden gestionar rutinas.";
                return RedirectToAction("Index", "Home");
            }

            // ? Obtener IDs de clientes que YA tienen evaluación (están en algún grupo)
            var clientesEnGrupos = await _context.EvaluacionesRendimiento
                .Select(e => e.ClienteId)
                .Distinct()
                .ToListAsync();

            // ? Mostrar SOLO clientes SIN evaluación (disponibles)
            var clientesActivos = await _context.Usuarios
                .Include(u => u.Rol)
                .Where(u => u.RolId == 1 && u.Activo && !clientesEnGrupos.Contains(u.UsuarioId))
                .OrderBy(u => u.Nombre)
                .ToListAsync();

            var rutinasRecientes = await _context.Rutinas
                .Include(r => r.Cliente)
                .Include(r => r.EvaluacionBase)
                .Where(r => r.EmpleadoId == empleadoId)
                .OrderByDescending(r => r.FechaCreacion)
                .Take(10)
                .ToListAsync();

            ViewData["ClientesActivos"] = clientesActivos;
            ViewData["RutinasRecientes"] = rutinasRecientes;
            ViewData["TotalClientesActivos"] = clientesActivos.Count;
            ViewData["EmpleadoNombre"] = usuario.Nombre;

            return View("~/Views/Empleado/Index.cshtml");
        }
        /// <summary>
        /// ? CORREGIDO: SeleccionarClientes - Solo clientes SIN evaluación
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> SeleccionarClientes()
        {
            var empleadoId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            var usuario = await _context.Usuarios
                .Include(u => u.Rol)
                .FirstOrDefaultAsync(u => u.UsuarioId == empleadoId);

            if (usuario?.RolId != 2)
            {
                TempData["Error"] = "Acceso denegado.";
                return RedirectToAction("Index", "Home");
            }

            // ? Obtener IDs de clientes en grupos (con evaluación)
            var clientesEnGrupos = await _context.EvaluacionesRendimiento
                .Select(e => e.ClienteId)
                .Distinct()
                .ToListAsync();

            // ? Mostrar SOLO clientes disponibles (sin evaluación)
            var clientesDisponibles = await _context.Usuarios
                .Include(u => u.Rol)
                .Where(u => u.RolId == 1 && u.Activo && !clientesEnGrupos.Contains(u.UsuarioId))
                .OrderBy(u => u.Nombre)
                .ToListAsync();

            ViewData["EmpleadoNombre"] = usuario.Nombre;
            ViewData["TotalDisponibles"] = clientesDisponibles.Count;
            ViewData["TotalEnGrupos"] = clientesEnGrupos.Count;

            return View("~/Views/Empleado/SeleccionarClientes.cshtml", clientesDisponibles);
        }

        /// <summary>
        /// ? ACTUALIZADO: ProcesarGrupo - Crear grupo con título y color
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ProcesarGrupo(List<int> clientesSeleccionados, string tituloGrupo, string colorGrupo)
        {
            var empleadoId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            var empleado = await _context.Usuarios
                .Include(u => u.Rol)
                .FirstOrDefaultAsync(u => u.UsuarioId == empleadoId);

            if (empleado?.RolId != 2)
            {
                TempData["Error"] = "Acceso denegado.";
                return RedirectToAction("Index", "Home");
            }

            if (clientesSeleccionados == null || !clientesSeleccionados.Any())
            {
                TempData["Error"] = "Debes seleccionar al menos un cliente.";
                return RedirectToAction(nameof(SeleccionarClientes));
            }

            // ? Validar título
            if (string.IsNullOrWhiteSpace(tituloGrupo))
            {
                TempData["Error"] = "El título del grupo es obligatorio.";
                return RedirectToAction(nameof(SeleccionarClientes));
            }

            // ? Verificar que NINGÚN cliente tenga evaluación previa
            var clientesConEvaluacion = await _context.EvaluacionesRendimiento
                .Where(e => clientesSeleccionados.Contains(e.ClienteId))
                .Include(e => e.Cliente)
                .Include(e => e.Empleado)
                .ToListAsync();

            if (clientesConEvaluacion.Any())
            {
                var detalles = string.Join(", ", clientesConEvaluacion.Select(e =>
                    $"{e.Cliente?.Nombre} (Grupo de: {e.Empleado?.Nombre})"));

                TempData["Error"] = $"Los siguientes clientes ya están en un grupo: {detalles}. Elimina sus evaluaciones primero.";
                return RedirectToAction(nameof(SeleccionarClientes));
            }

            // ? NUEVO: Crear el grupo primero
            var nuevoGrupo = new GrupoClientes
            {
                Titulo = tituloGrupo.Trim(),
                Color = string.IsNullOrWhiteSpace(colorGrupo) ? "#ffc107" : colorGrupo,
                EmpleadoId = empleadoId,
                FechaCreacion = DateTime.Now,
                Activo = true
            };

            _context.GruposClientes.Add(nuevoGrupo);
            await _context.SaveChangesAsync();

            // Guardar el grupo en TempData (ahora incluye el GrupoId)
            TempData["GrupoClientesId"] = nuevoGrupo.GrupoClientesId;
            TempData["GrupoClientes"] = string.Join(",", clientesSeleccionados);
            TempData["CantidadClientes"] = clientesSeleccionados.Count;

            TempData["Success"] = $"Grupo '{tituloGrupo}' creado con {clientesSeleccionados.Count} cliente(s). Comienza las evaluaciones.";

            return RedirectToAction(nameof(EvaluarCliente), new { clienteId = clientesSeleccionados.First() });
        }

        /// <summary>
        /// ? ACTUALIZADO: Ver mis grupos con información del grupo y rutinas
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> MisGrupos()
        {
            var empleadoId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            var empleado = await _context.Usuarios
                .Include(u => u.Rol)
                .FirstOrDefaultAsync(u => u.UsuarioId == empleadoId);

            if (empleado?.RolId != 2)
            {
                TempData["Error"] = "Acceso denegado.";
                return RedirectToAction("Index", "Home");
            }

            // ? INCLUIR GRUPO Y RUTINAS EN LA CONSULTA
            var evaluaciones = await _context.EvaluacionesRendimiento
                .Include(e => e.Cliente)
                .Include(e => e.Rutinas)
                    .ThenInclude(r => r.Ejercicios) // ? Incluir ejercicios
                .Include(e => e.Grupo) // ? INCLUIR GRUPO
                .Where(e => e.EmpleadoId == empleadoId)
                .OrderByDescending(e => e.FechaEvaluacion)
                .ToListAsync();

            // ? CAMBIO PRINCIPAL: Agrupar por GrupoClientesId en lugar de fecha
            var gruposEvaluaciones = evaluaciones
                .GroupBy(e => e.GrupoClientesId ?? 0) // Agrupar por grupo (0 si no tiene grupo)
                .Select(g => new
                {
                    GrupoId = g.Key,
                    Fecha = g.FirstOrDefault()?.FechaEvaluacion.Date ?? DateTime.Now.Date,
                    Evaluaciones = g.ToList(),
                    TotalClientes = g.Count(),
                    PromedioGrupo = g.Average(e => e.PromedioRendimiento)
                })
                .OrderByDescending(g => g.Fecha)
                .ToList();

            ViewData["EmpleadoNombre"] = empleado.Nombre;
            ViewData["GruposEvaluaciones"] = gruposEvaluaciones;

            return View("~/Views/Empleado/MisGrupos.cshtml", evaluaciones);
        }

        /// <summary>
        /// ? ACTUALIZADO: EliminarEvaluacion con confirmación mejorada
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EliminarEvaluacion(int evaluacionId, bool confirmarEliminacion = false)
        {
            var empleadoId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            var empleado = await _context.Usuarios
                .Include(u => u.Rol)
                .FirstOrDefaultAsync(u => u.UsuarioId == empleadoId);

            if (empleado?.RolId != 2)
            {
                TempData["Error"] = "Acceso denegado.";
                return RedirectToAction("Index", "Home");
            }

            var evaluacion = await _context.EvaluacionesRendimiento
                .Include(e => e.Rutinas)
                    .ThenInclude(r => r.Ejercicios)
                .Include(e => e.Cliente)
                .FirstOrDefaultAsync(e => e.EvaluacionRendimientoId == evaluacionId && e.EmpleadoId == empleadoId);

            if (evaluacion == null)
            {
                TempData["Error"] = "Evaluación no encontrada o no tienes permisos.";
                return RedirectToAction(nameof(MisGrupos));
            }

            var clienteId = evaluacion.ClienteId;
            var clienteNombre = evaluacion.Cliente?.Nombre;

            // ? Si tiene rutinas Y NO confirmó, mostrar SOLO UNA VEZ
            if (evaluacion.Rutinas != null && evaluacion.Rutinas.Any() && !confirmarEliminacion)
            {
                var rutinasNombres = string.Join(", ", evaluacion.Rutinas.Select(r => $"'{r.Nombre}'"));

                // ? CAMBIO: Usar ViewData en lugar de TempData para evitar persistencia
                ViewData["MostrarConfirmacion"] = true;
                ViewData["EvaluacionId"] = evaluacionId;
                ViewData["ClienteNombre"] = clienteNombre;
                ViewData["CantidadRutinas"] = evaluacion.Rutinas.Count;
                ViewData["RutinasNombres"] = rutinasNombres;

                // Regresar a la vista con la modal de confirmación
                var todasEvaluaciones = await _context.EvaluacionesRendimiento
                    .Include(e => e.Cliente)
                    .Include(e => e.Rutinas)
                        .ThenInclude(r => r.Ejercicios)
                    .Include(e => e.Grupo)
                    .Where(e => e.EmpleadoId == empleadoId)
                    .OrderByDescending(e => e.FechaEvaluacion)
                    .ToListAsync();

                var gruposEvaluaciones = todasEvaluaciones
                    .GroupBy(e => e.GrupoClientesId ?? 0)
                    .Select(g => new
                    {
                        GrupoId = g.Key,
                        Fecha = g.FirstOrDefault()?.FechaEvaluacion.Date ?? DateTime.Now.Date,
                        Evaluaciones = g.ToList(),
                        TotalClientes = g.Count(),
                        PromedioGrupo = g.Average(e => e.PromedioRendimiento)
                    })
                    .OrderByDescending(g => g.Fecha)
                    .ToList();

                ViewData["EmpleadoNombre"] = empleado.Nombre;
                ViewData["GruposEvaluaciones"] = gruposEvaluaciones;

                return View("~/Views/Empleado/MisGrupos.cshtml", todasEvaluaciones);
            }

            // ? Proceder con eliminación (confirmado o sin rutinas)
            if (evaluacion.Rutinas != null && evaluacion.Rutinas.Any())
            {
                foreach (var rutina in evaluacion.Rutinas.ToList())
                {
                    if (rutina.Ejercicios != null && rutina.Ejercicios.Any())
                    {
                        _context.Ejercicios.RemoveRange(rutina.Ejercicios);
                    }
                    _context.Rutinas.Remove(rutina);
                }
                await _context.SaveChangesAsync();
            }

            _context.EvaluacionesRendimiento.Remove(evaluacion);
            await _context.SaveChangesAsync();

            var verificacion = await _context.EvaluacionesRendimiento
                .Where(e => e.ClienteId == clienteId)
                .CountAsync();

            if (verificacion > 0)
            {
                TempData["Error"] = $"? Error: {clienteNombre} todavía tiene {verificacion} evaluación(es).";
            }
            else
            {
                TempData["Success"] = $"? {clienteNombre} eliminado del grupo correctamente.";
            }

            return RedirectToAction(nameof(MisGrupos));
        }

        /// <summary>
        /// ? ACTUALIZADO: EvaluarCliente - Permitir re-evaluación
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> EvaluarCliente(int clienteId)
        {
            var empleadoId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            var empleado = await _context.Usuarios
                .Include(u => u.Rol)
                .FirstOrDefaultAsync(u => u.UsuarioId == empleadoId);

            if (empleado?.RolId != 2)
            {
                TempData["Error"] = "Acceso denegado.";
                return RedirectToAction("Index", "Home");
            }

            var cliente = await _context.Usuarios
                .Include(u => u.Rol)
                .FirstOrDefaultAsync(u => u.UsuarioId == clienteId);

            if (cliente == null || cliente.RolId != 1)
            {
                TempData["Error"] = "Cliente no encontrado.";
                return RedirectToAction(nameof(SeleccionarClientes));
            }

            // ? Buscar evaluación existente
            var evaluacionExistente = await _context.EvaluacionesRendimiento
                .FirstOrDefaultAsync(e => e.ClienteId == clienteId);

            if (evaluacionExistente != null)
            {
                // ? Si ya tiene evaluación, preguntar si quiere re-evaluar
                TempData["Info"] = $"{cliente.Nombre} ya tiene una evaluación. ¿Deseas actualizarla?";
                return RedirectToAction(nameof(EditarEvaluacion), new { evaluacionId = evaluacionExistente.EvaluacionRendimientoId });
            }

            var totalAsistencias = await _context.Reservas
                .Where(r => r.UsuarioId == clienteId && r.Asistio == true)
                .CountAsync();

            var grupoClientes = TempData.Peek("GrupoClientes")?.ToString();
            var clientesIds = !string.IsNullOrEmpty(grupoClientes)
                ? grupoClientes.Split(',').Select(int.Parse).ToList()
                : new List<int>();

            var indexActual = clientesIds.IndexOf(clienteId);
            var totalGrupo = clientesIds.Count;

            ViewData["Cliente"] = cliente;
            ViewData["UltimaEvaluacion"] = null;
            ViewData["TotalAsistencias"] = totalAsistencias;
            ViewData["EmpleadoNombre"] = empleado.Nombre;
            ViewData["GrupoClientes"] = clientesIds;
            ViewData["IndexActual"] = indexActual + 1;
            ViewData["TotalGrupo"] = totalGrupo;
            ViewData["ClienteActualId"] = clienteId;

            var evaluacion = new EvaluacionRendimiento
            {
                ClienteId = clienteId,
                FechaEvaluacion = DateTime.Now,
                Fuerza = 5,
                Resistencia = 5,
                Flexibilidad = 5,
                Tecnica = 5,
                NivelGeneral = 5
            };

            return View("~/Views/Empleado/EvaluarCliente.cshtml", evaluacion);
        }


        /// <summary>
        /// ? NUEVO: Editar evaluación existente
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> EditarEvaluacion(int evaluacionId)
        {
            var empleadoId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            var empleado = await _context.Usuarios
                .Include(u => u.Rol)
                .FirstOrDefaultAsync(u => u.UsuarioId == empleadoId);

            if (empleado?.RolId != 2)
            {
                TempData["Error"] = "Acceso denegado.";
                return RedirectToAction("Index", "Home");
            }

            var evaluacion = await _context.EvaluacionesRendimiento
                .Include(e => e.Cliente)
                .FirstOrDefaultAsync(e => e.EvaluacionRendimientoId == evaluacionId && e.EmpleadoId == empleadoId);

            if (evaluacion == null)
            {
                TempData["Error"] = "Evaluación no encontrada o no tienes permisos.";
                return RedirectToAction(nameof(MisGrupos));
            }

            var totalAsistencias = await _context.Reservas
                .Where(r => r.UsuarioId == evaluacion.ClienteId && r.Asistio == true)
                .CountAsync();

            ViewData["Cliente"] = evaluacion.Cliente;
            ViewData["UltimaEvaluacion"] = evaluacion;
            ViewData["TotalAsistencias"] = totalAsistencias;
            ViewData["EmpleadoNombre"] = empleado.Nombre;
            ViewData["EsEdicion"] = true;

            return View("~/Views/Empleado/EvaluarCliente.cshtml", evaluacion);
        }

        /// <summary>
        /// ? NUEVO: Actualizar evaluación existente
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ActualizarEvaluacion(EvaluacionRendimiento model)
        {
            var empleadoId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            var empleado = await _context.Usuarios
                .Include(u => u.Rol)
                .FirstOrDefaultAsync(u => u.UsuarioId == empleadoId);

            if (empleado?.RolId != 2)
            {
                TempData["Error"] = "Acceso denegado.";
                return RedirectToAction("Index", "Home");
            }

            var evaluacionExistente = await _context.EvaluacionesRendimiento
                .FirstOrDefaultAsync(e => e.EvaluacionRendimientoId == model.EvaluacionRendimientoId && e.EmpleadoId == empleadoId);

            if (evaluacionExistente == null)
            {
                TempData["Error"] = "Evaluación no encontrada.";
                return RedirectToAction(nameof(MisGrupos));
            }

            if (!ModelState.IsValid)
            {
                var cliente = await _context.Usuarios.FindAsync(model.ClienteId);
                ViewData["Cliente"] = cliente;
                ViewData["EmpleadoNombre"] = empleado.Nombre;
                ViewData["EsEdicion"] = true;
                return View("~/Views/Empleado/EvaluarCliente.cshtml", model);
            }

            // Actualizar valores
            evaluacionExistente.Fuerza = model.Fuerza;
            evaluacionExistente.Resistencia = model.Resistencia;
            evaluacionExistente.Flexibilidad = model.Flexibilidad;
            evaluacionExistente.Tecnica = model.Tecnica;
            evaluacionExistente.NivelGeneral = model.NivelGeneral;
            evaluacionExistente.Peso = model.Peso;
            evaluacionExistente.Altura = model.Altura;
            evaluacionExistente.ObjetivoCliente = model.ObjetivoCliente;
            evaluacionExistente.Observaciones = model.Observaciones;
            evaluacionExistente.FechaEvaluacion = DateTime.Now; // Actualizar fecha

            // Recalcular IMC
            if (evaluacionExistente.Peso.HasValue && evaluacionExistente.Altura.HasValue && evaluacionExistente.Altura > 0)
            {
                var alturaMetros = evaluacionExistente.Altura.Value / 100;
                evaluacionExistente.IMC = evaluacionExistente.Peso.Value / (alturaMetros * alturaMetros);
            }

            _context.EvaluacionesRendimiento.Update(evaluacionExistente);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Evaluación actualizada exitosamente.";
            return RedirectToAction(nameof(MisGrupos));
        }

        /// <summary>
        /// ? ACTUALIZADO: GuardarEvaluacion con validación de datos
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GuardarEvaluacion(EvaluacionRendimiento model)
        {
            var empleadoId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            var empleado = await _context.Usuarios
                .Include(u => u.Rol)
                .FirstOrDefaultAsync(u => u.UsuarioId == empleadoId);

            if (empleado?.RolId != 2)
            {
                TempData["Error"] = "Acceso denegado.";
                return RedirectToAction("Index", "Home");
            }

            // ? Validación adicional del servidor
            if (model.Peso.HasValue && (model.Peso <= 0 || model.Peso > 500))
            {
                ModelState.AddModelError("Peso", "El peso debe ser mayor a 0 y menor a 500 kg");
            }

            if (model.Altura.HasValue && (model.Altura < 50 || model.Altura > 300))
            {
                ModelState.AddModelError("Altura", "La altura debe estar entre 50 y 300 cm");
            }

            if (model.EvaluacionRendimientoId > 0)
            {
                return await ActualizarEvaluacion(model);
            }

            var evaluacionExistente = await _context.EvaluacionesRendimiento
                .FirstOrDefaultAsync(e => e.ClienteId == model.ClienteId);

            if (evaluacionExistente != null)
            {
                model.EvaluacionRendimientoId = evaluacionExistente.EvaluacionRendimientoId;
                return await ActualizarEvaluacion(model);
            }

            if (!ModelState.IsValid)
            {
                var cliente = await _context.Usuarios.FindAsync(model.ClienteId);
                var totalAsistencias = await _context.Reservas
                    .Where(r => r.UsuarioId == model.ClienteId && r.Asistio == true)
                    .CountAsync();

                ViewData["Cliente"] = cliente;
                ViewData["EmpleadoNombre"] = empleado.Nombre;
                ViewData["TotalAsistencias"] = totalAsistencias;
                ViewData["UltimaEvaluacion"] = null;

                return View("~/Views/Empleado/EvaluarCliente.cshtml", model);
            }

            if (model.Peso.HasValue && model.Altura.HasValue && model.Altura > 0)
            {
                var alturaMetros = model.Altura.Value / 100;
                model.IMC = model.Peso.Value / (alturaMetros * alturaMetros);
            }

            model.EmpleadoId = empleadoId;
            model.FechaEvaluacion = DateTime.Now;

            // Asociar al grupo si existe
            var grupoId = TempData.Peek("GrupoClientesId") as int?;
            if (grupoId.HasValue)
            {
                model.GrupoClientesId = grupoId.Value;
            }

            _context.EvaluacionesRendimiento.Add(model);
            await _context.SaveChangesAsync();

            var grupoClientes = TempData.Peek("GrupoClientes")?.ToString();
            if (!string.IsNullOrEmpty(grupoClientes))
            {
                var clientesIds = grupoClientes.Split(',').Select(int.Parse).ToList();
                var indexActual = clientesIds.IndexOf(model.ClienteId);

                if (indexActual < clientesIds.Count - 1)
                {
                    var siguienteClienteId = clientesIds[indexActual + 1];
                    TempData["Success"] = $"Evaluación guardada. Cliente {indexActual + 1}/{clientesIds.Count} completado.";
                    return RedirectToAction(nameof(EvaluarCliente), new { clienteId = siguienteClienteId });
                }
                else
                {
                    TempData.Remove("GrupoClientes");
                    TempData.Remove("CantidadClientes");
                    TempData.Remove("GrupoClientesId");
                    TempData["Success"] = $"? ¡Grupo completo! Has evaluado {clientesIds.Count} clientes exitosamente.";
                    return RedirectToAction(nameof(MisGrupos));
                }
            }
            else
            {
                TempData["Success"] = "Evaluación guardada exitosamente.";
                return RedirectToAction(nameof(MisGrupos));
            }
        }

        /// <summary>
        /// ? NUEVO: Resumen del grupo evaluado
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> ResumenGrupo(int evaluacionId)
        {
            var empleadoId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            var empleado = await _context.Usuarios
                .Include(u => u.Rol)
                .FirstOrDefaultAsync(u => u.UsuarioId == empleadoId);

            if (empleado?.RolId != 2)
            {
                TempData["Error"] = "Acceso denegado.";
                return RedirectToAction("Index", "Home");
            }

            // Obtener las últimas evaluaciones del empleado (del grupo recién evaluado)
            var evaluacionesRecientes = await _context.EvaluacionesRendimiento
                .Include(e => e.Cliente)
                .Where(e => e.EmpleadoId == empleadoId)
                .OrderByDescending(e => e.FechaEvaluacion)
                .Take(10)
                .ToListAsync();

            ViewData["EmpleadoNombre"] = empleado.Nombre;
            return View("~/Views/Empleado/ResumenGrupo.cshtml", evaluacionesRecientes);
        }

        [HttpGet]
        public async Task<IActionResult> CrearRutina(int clienteId, int evaluacionId)
        {
            var empleadoId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            var empleado = await _context.Usuarios
                .Include(u => u.Rol)
                .FirstOrDefaultAsync(u => u.UsuarioId == empleadoId);

            if (empleado?.RolId != 2)
            {
                TempData["Error"] = "Acceso denegado.";
                return RedirectToAction("Index", "Home");
            }

            var cliente = await _context.Usuarios
                .Include(u => u.Rol)
                .FirstOrDefaultAsync(u => u.UsuarioId == clienteId);

            var evaluacion = await _context.EvaluacionesRendimiento
                .FirstOrDefaultAsync(e => e.EvaluacionRendimientoId == evaluacionId);

            if (cliente == null || evaluacion == null)
            {
                TempData["Error"] = "Datos no encontrados.";
                return RedirectToAction(nameof(Index));
            }

            var horariosFijos = await _context.HorariosFijos
                .Where(h => h.UsuarioId == clienteId && h.Activo)
                .OrderBy(h => h.DiaSemana)
                .ToListAsync();

            ViewData["Cliente"] = cliente;
            ViewData["Evaluacion"] = evaluacion;
            ViewData["HorariosFijos"] = horariosFijos;
            ViewData["EmpleadoNombre"] = empleado.Nombre;

            var promedio = evaluacion.PromedioRendimiento;
            var nivelSugerido = promedio <= 3 ? "Principiante" :
                               promedio <= 7 ? "Intermedio" : "Avanzado";

            var rutina = new Rutina
            {
                ClienteId = clienteId,
                EvaluacionRendimientoId = evaluacionId,
                NivelDificultad = nivelSugerido,
                DuracionSemanas = 4,
                FechaInicio = DateTime.Today,
                FechaFin = DateTime.Today.AddDays(28)
            };

            // ? Apuntar a Views/Empleado/CrearRutina.cshtml
            return View("~/Views/Empleado/CrearRutina.cshtml", rutina);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CrearRutina(Rutina model)
        {
            var empleadoId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            var empleado = await _context.Usuarios
                .Include(u => u.Rol)
                .FirstOrDefaultAsync(u => u.UsuarioId == empleadoId);

            if (empleado?.RolId != 2)
            {
                TempData["Error"] = "Acceso denegado.";
                return RedirectToAction("Index", "Home");
            }

            if (!ModelState.IsValid)
            {
                var cliente = await _context.Usuarios.FindAsync(model.ClienteId);
                var evaluacion = await _context.EvaluacionesRendimiento.FindAsync(model.EvaluacionRendimientoId);
                var horariosFijos = await _context.HorariosFijos
                    .Where(h => h.UsuarioId == model.ClienteId && h.Activo)
                    .OrderBy(h => h.DiaSemana)
                    .ToListAsync();

                ViewData["Cliente"] = cliente;
                ViewData["Evaluacion"] = evaluacion;
                ViewData["HorariosFijos"] = horariosFijos;
                ViewData["EmpleadoNombre"] = empleado.Nombre;

                // ? Cambiar aquí también
                return View("~/Views/Empleado/CrearRutina.cshtml", model);
            }

            model.EmpleadoId = empleadoId;
            model.FechaCreacion = DateTime.Now;
            model.Activa = true;

            if (model.FechaInicio.HasValue && !model.FechaFin.HasValue)
            {
                model.FechaFin = model.FechaInicio.Value.AddDays(model.DuracionSemanas * 7);
            }

            _context.Rutinas.Add(model);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"? Rutina creada exitosamente por {empleado.Nombre}.";
            return RedirectToAction(nameof(ListarRutinas));
        }

        [HttpGet]
        public async Task<IActionResult> ListarRutinas()
        {
            var empleadoId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            var empleado = await _context.Usuarios
                .Include(u => u.Rol)
                .FirstOrDefaultAsync(u => u.UsuarioId == empleadoId);

            if (empleado?.RolId != 2)
            {
                TempData["Error"] = "Acceso denegado.";
                return RedirectToAction("Index", "Home");
            }

            var rutinas = await _context.Rutinas
                .Include(r => r.Cliente)
                .Include(r => r.Empleado)
                .Include(r => r.EvaluacionBase)
                .Where(r => r.EmpleadoId == empleadoId)
                .OrderByDescending(r => r.FechaCreacion)
                .ToListAsync();

            ViewData["EmpleadoNombre"] = empleado.Nombre;

            // ? Apuntar a Views/Empleado/ListarRutinas.cshtml
            return View("~/Views/Empleado/ListarRutinas.cshtml", rutinas);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DesactivarRutina(int id)
        {
            var empleadoId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            var empleado = await _context.Usuarios
                .Include(u => u.Rol)
                .FirstOrDefaultAsync(u => u.UsuarioId == empleadoId);

            if (empleado?.RolId != 2)
            {
                TempData["Error"] = "Acceso denegado.";
                return RedirectToAction("Index", "Home");
            }

            var rutina = await _context.Rutinas
                .FirstOrDefaultAsync(r => r.RutinaId == id && r.EmpleadoId == empleadoId);

            if (rutina == null)
            {
                TempData["Error"] = "Rutina no encontrada o no tienes permisos.";
                return RedirectToAction(nameof(ListarRutinas));
            }

            rutina.Activa = false;
            await _context.SaveChangesAsync();

            TempData["Success"] = "Rutina desactivada exitosamente.";
            return RedirectToAction(nameof(ListarRutinas));
        }
        /// <summary>
        /// ? ACTUALIZADO: Ver calendario con colores únicos por cliente
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> CalendarioSemanal()
        {
            var empleadoId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            var empleado = await _context.Usuarios
                .Include(u => u.Rol)
                .FirstOrDefaultAsync(u => u.UsuarioId == empleadoId);

            if (empleado?.RolId != 2)
            {
                TempData["Error"] = "Acceso denegado.";
                return RedirectToAction("Index", "Home");
            }

            // ? Obtener evaluaciones CON grupo
            var evaluaciones = await _context.EvaluacionesRendimiento
                .Include(e => e.Cliente)
                .Include(e => e.Grupo)
                .Where(e => e.EmpleadoId == empleadoId)
                .ToListAsync();

            var clientesIds = evaluaciones.Select(e => e.ClienteId).Distinct().ToList();

            if (!clientesIds.Any())
            {
                TempData["Warning"] = "No tienes clientes evaluados aún. Ve a 'Mis Grupos' para evaluar clientes.";
                ViewData["EmpleadoNombre"] = empleado.Nombre;
                ViewData["HorariosFijos"] = new List<HorarioFijo>();
                ViewData["ReservasFuturas"] = new List<Reserva>();
                ViewData["Rutinas"] = new List<Rutina>();
                ViewData["InicioSemana"] = DateTime.Today;
                ViewData["ColoresClientes"] = new Dictionary<int, string>();
                return View("~/Views/Empleado/CalendarioSemanal.cshtml");
            }

            // ? NUEVO: Generar colores únicos por cliente
            var coloresPredefinidos = new List<string>
    {
        "#FF6B6B", // Rojo
        "#4ECDC4", // Turquesa
        "#45B7D1", // Azul claro
        "#FFA07A", // Salmón
        "#98D8C8", // Verde menta
        "#F7DC6F", // Amarillo
        "#BB8FCE", // Morado
        "#85C1E2", // Azul cielo
        "#F8B739", // Naranja
        "#52B788", // Verde
        "#E76F51", // Terracota
        "#2A9D8F", // Verde azulado
        "#E9C46A", // Dorado
        "#F4A261", // Naranja claro
        "#264653"  // Azul oscuro
    };

            var coloresClientes = new Dictionary<int, string>();
            for (int i = 0; i < clientesIds.Count; i++)
            {
                // Asignar color del grupo SI existe, sino usar colores predefinidos rotativos
                var evaluacion = evaluaciones.FirstOrDefault(e => e.ClienteId == clientesIds[i]);
                if (evaluacion?.Grupo != null)
                {
                    // Si el grupo tiene color, usarlo como base y modificarlo ligeramente por cliente
                    var colorBase = evaluacion.Grupo.Color;
                    coloresClientes[clientesIds[i]] = GenerarVariacionColor(colorBase, i);
                }
                else
                {
                    // Si no tiene grupo, usar colores predefinidos
                    coloresClientes[clientesIds[i]] = coloresPredefinidos[i % coloresPredefinidos.Count];
                }
            }

            var horariosFijos = await _context.HorariosFijos
                .Include(h => h.Usuario)
                .Where(h => clientesIds.Contains(h.UsuarioId) && h.Activo)
                .ToListAsync();

            var inicioSemana = DateTime.Today;
            var finSemana = inicioSemana.AddDays(7);

            var reservasFuturas = await _context.Reservas
                .Include(r => r.Usuario)
                .Where(r => clientesIds.Contains(r.UsuarioId) &&
                           r.FechaReserva >= inicioSemana &&
                           r.FechaReserva < finSemana &&
                           r.Estado != EstadoReserva.Cancelada)
                .ToListAsync();

            var rutinas = await _context.Rutinas
                .Include(r => r.Cliente)
                .Include(r => r.Ejercicios)
                .Where(r => clientesIds.Contains(r.ClienteId) &&
                           r.EmpleadoId == empleadoId &&
                           r.Activa)
                .ToListAsync();

            ViewData["EmpleadoNombre"] = empleado.Nombre;
            ViewData["HorariosFijos"] = horariosFijos;
            ViewData["ReservasFuturas"] = reservasFuturas;
            ViewData["Rutinas"] = rutinas;
            ViewData["InicioSemana"] = inicioSemana;
            ViewData["TotalClientes"] = clientesIds.Count;
            ViewData["ColoresClientes"] = coloresClientes; // ? Colores únicos por cliente

            return View("~/Views/Empleado/CalendarioSemanal.cshtml");
        }

        /// <summary>
        /// ? NUEVO: Generar variación de color para diferenciar clientes del mismo grupo
        /// </summary>
        private string GenerarVariacionColor(string colorBase, int index)
        {
            // Extraer componentes RGB del color hexadecimal
            var r = Convert.ToInt32(colorBase.Substring(1, 2), 16);
            var g = Convert.ToInt32(colorBase.Substring(3, 2), 16);
            var b = Convert.ToInt32(colorBase.Substring(5, 2), 16);

            // Aplicar variación según el índice
            switch (index % 5)
            {
                case 0: // Original
                    return colorBase;
                case 1: // Más claro
                    r = Math.Min(255, r + 40);
                    g = Math.Min(255, g + 40);
                    b = Math.Min(255, b + 40);
                    break;
                case 2: // Más oscuro
                    r = Math.Max(0, r - 40);
                    g = Math.Max(0, g - 40);
                    b = Math.Max(0, b - 40);
                    break;
                case 3: // Variación hacia rojo
                    r = Math.Min(255, r + 50);
                    g = Math.Max(0, g - 20);
                    b = Math.Max(0, b - 20);
                    break;
                case 4: // Variación hacia azul
                    r = Math.Max(0, r - 20);
                    g = Math.Max(0, g - 20);
                    b = Math.Min(255, b + 50);
                    break;
            }

            return $"#{r:X2}{g:X2}{b:X2}";
        }
        /// <summary>
        /// ? NUEVO: Eliminar rutina
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EliminarRutina(int rutinaId)
        {
            var empleadoId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            var empleado = await _context.Usuarios
                .Include(u => u.Rol)
                .FirstOrDefaultAsync(u => u.UsuarioId == empleadoId);

            if (empleado?.RolId != 2)
            {
                TempData["Error"] = "Acceso denegado.";
                return RedirectToAction("Index", "Home");
            }

            var rutina = await _context.Rutinas
                .Include(r => r.Cliente)
                .Include(r => r.Ejercicios)
                .FirstOrDefaultAsync(r => r.RutinaId == rutinaId && r.EmpleadoId == empleadoId);

            if (rutina == null)
            {
                TempData["Error"] = "Rutina no encontrada o no tienes permisos.";
                return RedirectToAction(nameof(MisGrupos));
            }

            var clienteNombre = rutina.Cliente?.Nombre;

            // Eliminar ejercicios asociados primero (si existen)
            if (rutina.Ejercicios != null && rutina.Ejercicios.Any())
            {
                _context.Ejercicios.RemoveRange(rutina.Ejercicios);
            }

            // Eliminar la rutina
            _context.Rutinas.Remove(rutina);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Rutina '{rutina.Nombre}' de {clienteNombre} eliminada exitosamente.";
            return RedirectToAction(nameof(MisGrupos));
        }
        // Agregar este método dentro de GestionRutinasController

        /// <summary>
        /// ? NUEVO: Seleccionar cliente desde el calendario para crear rutina
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> SeleccionarClienteParaRutina(int clienteId, int? diaSemana, int? hora, string? fecha)
        {
            var empleadoId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            var empleado = await _context.Usuarios
                .Include(u => u.Rol)
                .FirstOrDefaultAsync(u => u.UsuarioId == empleadoId);

            if (empleado?.RolId != 2)
            {
                TempData["Error"] = "Acceso denegado.";
                return RedirectToAction("Index", "Home");
            }

            // Verificar que el cliente esté en el grupo del empleado (tenga evaluación)
            var evaluacion = await _context.EvaluacionesRendimiento
                .Where(e => e.ClienteId == clienteId && e.EmpleadoId == empleadoId)
                .OrderByDescending(e => e.FechaEvaluacion)
                .FirstOrDefaultAsync();

            if (evaluacion == null)
            {
                TempData["Error"] = "Este cliente no está en tu grupo. Debes evaluarlo primero.";
                return RedirectToAction(nameof(CalendarioSemanal));
            }

            // Verificar si ya tiene rutina
            var tieneRutina = await _context.Rutinas
                .AnyAsync(r => r.ClienteId == clienteId && r.EmpleadoId == empleadoId && r.Activa);

            if (tieneRutina)
            {
                TempData["Warning"] = "Este cliente ya tiene una rutina activa.";
                return RedirectToAction(nameof(CalendarioSemanal));
            }

            // Redirigir directamente a crear rutina
            TempData["OrigenCalendario"] = "true";
            TempData["DiaSemana"] = diaSemana?.ToString();
            TempData["Hora"] = hora?.ToString();
            TempData["Fecha"] = fecha;

            return RedirectToAction(nameof(CrearRutina), new { clienteId, evaluacionId = evaluacion.EvaluacionRendimientoId });
        }
        /// <summary>
        /// ? NUEVO: Crear rutina para un día/horario específico
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> CrearRutinaEspecifica(
            int clienteId,
            int? diaSemana,
            string? horaInicio,
            string? horaFin,
            string? tipoEntrenamiento,
            string? fechaEspecifica)
        {
            var empleadoId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            var empleado = await _context.Usuarios
                .Include(u => u.Rol)
                .FirstOrDefaultAsync(u => u.UsuarioId == empleadoId);

            if (empleado?.RolId != 2)
            {
                TempData["Error"] = "Acceso denegado.";
                return RedirectToAction("Index", "Home");
            }

            // Verificar que el cliente esté en el grupo del empleado
            var evaluacion = await _context.EvaluacionesRendimiento
                .Where(e => e.ClienteId == clienteId && e.EmpleadoId == empleadoId)
                .OrderByDescending(e => e.FechaEvaluacion)
                .FirstOrDefaultAsync();

            if (evaluacion == null)
            {
                TempData["Error"] = "Este cliente no está en tu grupo.";
                return RedirectToAction(nameof(CalendarioSemanal));
            }

            var cliente = await _context.Usuarios.FindAsync(clienteId);

            var rutina = new Rutina
            {
                ClienteId = clienteId,
                EvaluacionRendimientoId = evaluacion.EvaluacionRendimientoId,
                NivelDificultad = evaluacion.PromedioRendimiento <= 3 ? "Principiante" :
                                 evaluacion.PromedioRendimiento <= 7 ? "Intermedio" : "Avanzado",
                DuracionSemanas = 4,
                FechaInicio = DateTime.Today,
                FechaFin = DateTime.Today.AddDays(28),
                Tipo = tipoEntrenamiento ?? "General",
                Nombre = $"Rutina {tipoEntrenamiento ?? "General"} - {cliente?.Nombre}"
            };

            // Establecer día y hora específicos
            if (diaSemana.HasValue)
            {
                rutina.DiaSemana = (DayOfWeek)diaSemana.Value;
            }

            if (!string.IsNullOrEmpty(horaInicio))
            {
                rutina.HoraInicio = TimeSpan.Parse(horaInicio);
            }

            if (!string.IsNullOrEmpty(horaFin))
            {
                rutina.HoraFin = TimeSpan.Parse(horaFin);
            }

            if (!string.IsNullOrEmpty(fechaEspecifica))
            {
                rutina.FechaEspecifica = DateTime.Parse(fechaEspecifica);
            }

            ViewData["Cliente"] = cliente;
            ViewData["Evaluacion"] = evaluacion;
            ViewData["EmpleadoNombre"] = empleado.Nombre;
            ViewData["DiaSemana"] = diaSemana.HasValue ? ((DayOfWeek)diaSemana.Value).ToString() : null;
            ViewData["HoraInicio"] = horaInicio;
            ViewData["HoraFin"] = horaFin;

            return View("~/Views/Empleado/CrearRutinaEspecifica.cshtml", rutina);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CrearRutinaEspecifica(Rutina model)
        {
            var empleadoId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            var empleado = await _context.Usuarios
                .Include(u => u.Rol)
                .FirstOrDefaultAsync(u => u.UsuarioId == empleadoId);

            if (empleado?.RolId != 2)
            {
                TempData["Error"] = "Acceso denegado.";
                return RedirectToAction("Index", "Home");
            }

            model.EmpleadoId = empleadoId;
            model.FechaCreacion = DateTime.Now;
            model.Activa = true;

            // ? Limpiar y recrear la colección
            model.Ejercicios = new List<Ejercicio>();

            // Procesar ejercicios del formulario
            var ejerciciosNombres = Request.Form.Keys
                .Where(k => k.StartsWith("ejercicios[") && k.EndsWith("].Nombre"))
                .ToList();

            var nombresUnicos = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var key in ejerciciosNombres)
            {
                var indexStr = key.Replace("ejercicios[", "").Replace("].Nombre", "");
                if (int.TryParse(indexStr, out int index))
                {
                    var nombre = Request.Form[$"ejercicios[{index}].Nombre"].ToString().Trim();

                    if (!string.IsNullOrWhiteSpace(nombre))
                    {
                        // ? VALIDACIÓN: Sin espacios
                        if (nombre.Contains(" "))
                        {
                            TempData["Error"] = $"? El nombre '{nombre}' no puede contener espacios. Usa guiones o escribe sin separación.";
                            var cliente = await _context.Usuarios.FindAsync(model.ClienteId);
                            var evaluacion = await _context.EvaluacionesRendimiento.FindAsync(model.EvaluacionRendimientoId);
                            ViewData["Cliente"] = cliente;
                            ViewData["Evaluacion"] = evaluacion;
                            ViewData["EmpleadoNombre"] = empleado.Nombre;
                            return View("~/Views/Empleado/CrearRutinaEspecifica.cshtml", model);
                        }

                        // ? VALIDACIÓN: Sin símbolos
                        if (!System.Text.RegularExpressions.Regex.IsMatch(nombre, @"^[a-zA-ZáéíóúÁÉÍÓÚñÑ]+$"))
                        {
                            TempData["Error"] = $"? El nombre '{nombre}' solo puede contener letras (sin números, símbolos ni espacios).";
                            var cliente = await _context.Usuarios.FindAsync(model.ClienteId);
                            var evaluacion = await _context.EvaluacionesRendimiento.FindAsync(model.EvaluacionRendimientoId);
                            ViewData["Cliente"] = cliente;
                            ViewData["Evaluacion"] = evaluacion;
                            ViewData["EmpleadoNombre"] = empleado.Nombre;
                            return View("~/Views/Empleado/CrearRutinaEspecifica.cshtml", model);
                        }

                        // ? VALIDACIÓN: Duplicados
                        if (nombresUnicos.Contains(nombre))
                        {
                            TempData["Error"] = $"? El ejercicio '{nombre}' aparece más de una vez. Cada ejercicio debe tener un nombre único.";
                            var cliente = await _context.Usuarios.FindAsync(model.ClienteId);
                            var evaluacion = await _context.EvaluacionesRendimiento.FindAsync(model.EvaluacionRendimientoId);
                            ViewData["Cliente"] = cliente;
                            ViewData["Evaluacion"] = evaluacion;
                            ViewData["EmpleadoNombre"] = empleado.Nombre;
                            return View("~/Views/Empleado/CrearRutinaEspecifica.cshtml", model);
                        }

                        nombresUnicos.Add(nombre);

                        var ejercicio = new Ejercicio
                        {
                            Nombre = nombre,
                            GrupoMuscular = Request.Form[$"ejercicios[{index}].GrupoMuscular"].ToString(),
                            Series = int.TryParse(Request.Form[$"ejercicios[{index}].Series"], out int series) ? series : 3,
                            Repeticiones = int.TryParse(Request.Form[$"ejercicios[{index}].Repeticiones"], out int reps) ? reps : 10,
                            Duracion = Request.Form[$"ejercicios[{index}].Duracion"].ToString(),
                            Notas = Request.Form[$"ejercicios[{index}].Notas"].ToString()
                        };

                        if (string.IsNullOrWhiteSpace(ejercicio.GrupoMuscular))
                        {
                            ejercicio.GrupoMuscular = model.Tipo;
                        }

                        model.Ejercicios.Add(ejercicio);
                    }
                }
            }

            if (model.Ejercicios.Count == 0)
            {
                TempData["Error"] = "Debes agregar al menos un ejercicio.";
                var cliente = await _context.Usuarios.FindAsync(model.ClienteId);
                var evaluacion = await _context.EvaluacionesRendimiento.FindAsync(model.EvaluacionRendimientoId);
                ViewData["Cliente"] = cliente;
                ViewData["Evaluacion"] = evaluacion;
                ViewData["EmpleadoNombre"] = empleado.Nombre;
                return View("~/Views/Empleado/CrearRutinaEspecifica.cshtml", model);
            }

            try
            {
                _context.ChangeTracker.Clear();
                _context.Rutinas.Add(model);
                await _context.SaveChangesAsync();

                TempData["Success"] = $"? Rutina '{model.Nombre}' creada con {model.Ejercicios.Count} ejercicio(s).";
                return RedirectToAction(nameof(CalendarioSemanal));
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error al crear rutina: {ex.Message}";
                var cliente = await _context.Usuarios.FindAsync(model.ClienteId);
                var evaluacion = await _context.EvaluacionesRendimiento.FindAsync(model.EvaluacionRendimientoId);
                ViewData["Cliente"] = cliente;
                ViewData["Evaluacion"] = evaluacion;
                ViewData["EmpleadoNombre"] = empleado.Nombre;
                return View("~/Views/Empleado/CrearRutinaEspecifica.cshtml", model);
            }
        }

        /// <summary>
        /// ? NUEVO: Ver detalle de rutina con ejercicios
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> VerRutinaDetalle(int rutinaId)
        {
            var empleadoId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            var rutina = await _context.Rutinas
                .Include(r => r.Cliente)
                .Include(r => r.Empleado)
                .Include(r => r.Ejercicios)
                .Include(r => r.EvaluacionBase)
                .FirstOrDefaultAsync(r => r.RutinaId == rutinaId && r.EmpleadoId == empleadoId);

            if (rutina == null)
            {
                TempData["Error"] = "Rutina no encontrada.";
                return RedirectToAction(nameof(CalendarioSemanal));
            }

            return View("~/Views/Empleado/DetalleRutina.cshtml", rutina);
        }
        // Agregar estos métodos al final de GestionRutinasController

        /// <summary>
        /// ? NUEVO: Eliminar ejercicio individual
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EliminarEjercicio(int ejercicioId, int rutinaId)
        {
            var empleadoId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            var ejercicio = await _context.Ejercicios
                .Include(e => e.Rutina)
                .FirstOrDefaultAsync(e => e.EjercicioId == ejercicioId && e.Rutina.EmpleadoId == empleadoId);

            if (ejercicio == null)
            {
                TempData["Error"] = "Ejercicio no encontrado o no tienes permisos.";
                return RedirectToAction(nameof(VerRutinaDetalle), new { rutinaId });
            }

            _context.Ejercicios.Remove(ejercicio);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Ejercicio '{ejercicio.Nombre}' eliminado exitosamente.";
            return RedirectToAction(nameof(VerRutinaDetalle), new { rutinaId });
        }

        /// <summary>
        /// ? NUEVO: Editar ejercicio - GET
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> EditarEjercicio(int ejercicioId)
        {
            var empleadoId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            var ejercicio = await _context.Ejercicios
                .Include(e => e.Rutina)
                    .ThenInclude(r => r.Cliente)
                .FirstOrDefaultAsync(e => e.EjercicioId == ejercicioId && e.Rutina.EmpleadoId == empleadoId);

            if (ejercicio == null)
            {
                TempData["Error"] = "Ejercicio no encontrado.";
                return RedirectToAction(nameof(CalendarioSemanal));
            }

            var empleado = await _context.Usuarios.FindAsync(empleadoId);
            ViewData["EmpleadoNombre"] = empleado?.Nombre;
            ViewData["Rutina"] = ejercicio.Rutina;

            return View("~/Views/Empleado/EditarEjercicio.cshtml", ejercicio);
        }

        /// <summary>
        /// ? NUEVO: Editar ejercicio - POST
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditarEjercicio(Ejercicio model)
        {
            var empleadoId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            var ejercicio = await _context.Ejercicios
                .Include(e => e.Rutina)
                .FirstOrDefaultAsync(e => e.EjercicioId == model.EjercicioId && e.Rutina.EmpleadoId == empleadoId);

            if (ejercicio == null)
            {
                TempData["Error"] = "Ejercicio no encontrado.";
                return RedirectToAction(nameof(CalendarioSemanal));
            }

            if (!ModelState.IsValid)
            {
                var empleado = await _context.Usuarios.FindAsync(empleadoId);
                ViewData["EmpleadoNombre"] = empleado?.Nombre;
                ViewData["Rutina"] = ejercicio.Rutina;
                return View("~/Views/Empleado/EditarEjercicio.cshtml", model);
            }

            ejercicio.Nombre = model.Nombre;
            ejercicio.Series = model.Series;
            ejercicio.Repeticiones = model.Repeticiones;
            ejercicio.Duracion = model.Duracion;
            ejercicio.Notas = model.Notas;
            ejercicio.GrupoMuscular = model.GrupoMuscular;

            _context.Ejercicios.Update(ejercicio);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Ejercicio '{ejercicio.Nombre}' actualizado exitosamente.";
            return RedirectToAction(nameof(VerRutinaDetalle), new { rutinaId = ejercicio.RutinaId });
        }
        /// <summary>
        /// ? ACTUALIZADO: Agregar ejercicio con validación estricta de nombre
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AgregarEjercicio(int rutinaId, Ejercicio model)
        {
            var empleadoId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            var rutina = await _context.Rutinas
                .Include(r => r.Ejercicios)
                .FirstOrDefaultAsync(r => r.RutinaId == rutinaId && r.EmpleadoId == empleadoId);

            if (rutina == null)
            {
                TempData["Error"] = "Rutina no encontrada.";
                return RedirectToAction(nameof(CalendarioSemanal));
            }

            // ? VALIDACIÓN 1: Nombre vacío
            if (string.IsNullOrWhiteSpace(model.Nombre))
            {
                TempData["Error"] = "? El nombre del ejercicio es obligatorio.";
                return RedirectToAction(nameof(VerRutinaDetalle), new { rutinaId });
            }

            // ? VALIDACIÓN 2: Sin espacios
            if (model.Nombre.Contains(" "))
            {
                TempData["Error"] = $"? El nombre '{model.Nombre}' no puede contener espacios. Usa guiones o escribe sin separación (ej: 'PressHombros', 'Sentadillas').";
                return RedirectToAction(nameof(VerRutinaDetalle), new { rutinaId });
            }

            // ? VALIDACIÓN 3: Sin símbolos (solo letras)
            if (!System.Text.RegularExpressions.Regex.IsMatch(model.Nombre, @"^[a-zA-ZáéíóúÁÉÍÓÚñÑ]+$"))
            {
                TempData["Error"] = $"? El nombre '{model.Nombre}' solo puede contener letras (sin números, símbolos ni espacios).";
                return RedirectToAction(nameof(VerRutinaDetalle), new { rutinaId });
            }

            // ? VALIDACIÓN 4: Normalizar y verificar duplicados
            var nombreNormalizado = model.Nombre.Trim().ToLower();

            var ejercicioDuplicado = rutina.Ejercicios?.Any(e =>
                !string.IsNullOrWhiteSpace(e.Nombre) &&
                e.Nombre.Trim().ToLower() == nombreNormalizado);

            if (ejercicioDuplicado == true)
            {
                TempData["Error"] = $"? Ya existe un ejercicio llamado '{model.Nombre}' en esta rutina. Usa un nombre diferente.";
                return RedirectToAction(nameof(VerRutinaDetalle), new { rutinaId });
            }

            model.RutinaId = rutinaId;
            model.Nombre = model.Nombre.Trim(); // Normalizar antes de guardar

            if (string.IsNullOrWhiteSpace(model.GrupoMuscular))
            {
                model.GrupoMuscular = rutina.Tipo;
            }

            _context.Ejercicios.Add(model);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"? Ejercicio '{model.Nombre}' agregado exitosamente.";
            return RedirectToAction(nameof(VerRutinaDetalle), new { rutinaId });
        }
        /// <summary>
        /// ? NUEVO: Visualizar TODOS los clientes activos (solo lectura)
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> VisualizarClientes()
        {
            var empleadoId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            var usuario = await _context.Usuarios
                .Include(u => u.Rol)
                .FirstOrDefaultAsync(u => u.UsuarioId == empleadoId);

            if (usuario?.RolId != 2)
            {
                TempData["Error"] = "Acceso denegado.";
                return RedirectToAction("Index", "Home");
            }

            // ? Mostrar TODOS los clientes activos (sin filtros)
            var clientesActivos = await _context.Usuarios
                .Include(u => u.Rol)
                .Where(u => u.RolId == 1 && u.Activo)
                .OrderBy(u => u.Nombre)
                .ToListAsync();

            ViewData["EmpleadoNombre"] = usuario.Nombre;
            ViewData["EsSoloVisualizacion"] = true; // ? Flag para la vista

            return View("~/Views/Empleado/VisualizarClientes.cshtml", clientesActivos);
        }
        /// <summary>
        /// ? NUEVO: Editar título y color de un grupo
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditarGrupo(int grupoId, string titulo, string color)
        {
            var empleadoId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            var empleado = await _context.Usuarios
                .Include(u => u.Rol)
                .FirstOrDefaultAsync(u => u.UsuarioId == empleadoId);

            if (empleado?.RolId != 2)
            {
                TempData["Error"] = "Acceso denegado.";
                return RedirectToAction("Index", "Home");
            }

            var grupo = await _context.GruposClientes
                .FirstOrDefaultAsync(g => g.GrupoClientesId == grupoId && g.EmpleadoId == empleadoId);

            if (grupo == null)
            {
                TempData["Error"] = "Grupo no encontrado o no tienes permisos.";
                return RedirectToAction(nameof(MisGrupos));
            }

            if (string.IsNullOrWhiteSpace(titulo))
            {
                TempData["Error"] = "El título del grupo es obligatorio.";
                return RedirectToAction(nameof(MisGrupos));
            }

            // Actualizar grupo
            grupo.Titulo = titulo.Trim();
            grupo.Color = string.IsNullOrWhiteSpace(color) ? "#ffc107" : color;

            _context.GruposClientes.Update(grupo);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Grupo '{grupo.Titulo}' actualizado exitosamente.";
            return RedirectToAction(nameof(MisGrupos));
        }
        /// <summary>
        /// ? NUEVO: Seleccionar clientes para agregar a un grupo existente
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> AgregarClientesAGrupo(int grupoId)
        {
            var empleadoId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            var empleado = await _context.Usuarios
                .Include(u => u.Rol)
                .FirstOrDefaultAsync(u => u.UsuarioId == empleadoId);

            if (empleado?.RolId != 2)
            {
                TempData["Error"] = "Acceso denegado.";
                return RedirectToAction("Index", "Home");
            }

            var grupo = await _context.GruposClientes
                .Include(g => g.Evaluaciones)
                    .ThenInclude(e => e.Cliente)
                .FirstOrDefaultAsync(g => g.GrupoClientesId == grupoId && g.EmpleadoId == empleadoId);

            if (grupo == null)
            {
                TempData["Error"] = "Grupo no encontrado.";
                return RedirectToAction(nameof(MisGrupos));
            }

            // ? Obtener clientes que YA están en ALGÚN grupo (con evaluación)
            var clientesEnGrupos = await _context.EvaluacionesRendimiento
                .Select(e => e.ClienteId)
                .Distinct()
                .ToListAsync();

            // ? Mostrar SOLO clientes disponibles (sin evaluación)
            var clientesDisponibles = await _context.Usuarios
                .Include(u => u.Rol)
                .Where(u => u.RolId == 1 && u.Activo && !clientesEnGrupos.Contains(u.UsuarioId))
                .OrderBy(u => u.Nombre)
                .ToListAsync();

            ViewData["Grupo"] = grupo;
            ViewData["EmpleadoNombre"] = empleado.Nombre;
            ViewData["ClientesDisponibles"] = clientesDisponibles;
            ViewData["ClientesActualesGrupo"] = grupo.Evaluaciones?.Select(e => e.Cliente?.Nombre).ToList();

            return View("~/Views/Empleado/AgregarClientesAGrupo.cshtml", clientesDisponibles);
        }

        /// <summary>
        /// ? NUEVO: Procesar la adición de clientes a un grupo existente
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AgregarClientesAGrupo(int grupoId, List<int> clientesSeleccionados)
        {
            var empleadoId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            var empleado = await _context.Usuarios
                .Include(u => u.Rol)
                .FirstOrDefaultAsync(u => u.UsuarioId == empleadoId);

            if (empleado?.RolId != 2)
            {
                TempData["Error"] = "Acceso denegado.";
                return RedirectToAction("Index", "Home");
            }

            var grupo = await _context.GruposClientes
                .FirstOrDefaultAsync(g => g.GrupoClientesId == grupoId && g.EmpleadoId == empleadoId);

            if (grupo == null)
            {
                TempData["Error"] = "Grupo no encontrado.";
                return RedirectToAction(nameof(MisGrupos));
            }

            if (clientesSeleccionados == null || !clientesSeleccionados.Any())
            {
                TempData["Error"] = "Debes seleccionar al menos un cliente.";
                return RedirectToAction(nameof(AgregarClientesAGrupo), new { grupoId });
            }

            // ? Verificar que NINGÚN cliente tenga evaluación previa
            var clientesConEvaluacion = await _context.EvaluacionesRendimiento
                .Where(e => clientesSeleccionados.Contains(e.ClienteId))
                .Include(e => e.Cliente)
                .ToListAsync();

            if (clientesConEvaluacion.Any())
            {
                var nombres = string.Join(", ", clientesConEvaluacion.Select(e => e.Cliente?.Nombre));
                TempData["Error"] = $"Los siguientes clientes ya están en un grupo: {nombres}";
                return RedirectToAction(nameof(AgregarClientesAGrupo), new { grupoId });
            }

            // ? Guardar info del grupo para las evaluaciones
            TempData["GrupoClientesId"] = grupoId;
            TempData["GrupoClientes"] = string.Join(",", clientesSeleccionados);
            TempData["CantidadClientes"] = clientesSeleccionados.Count;

            TempData["Success"] = $"Seleccionados {clientesSeleccionados.Count} cliente(s) para agregar al grupo '{grupo.Titulo}'.";

            return RedirectToAction(nameof(EvaluarCliente), new { clienteId = clientesSeleccionados.First() });
        }
    }
}