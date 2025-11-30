using GYM.Data;
using GYM.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace GYM.Controllers
{
    [Authorize(Roles = "Cliente")]
    public class PlanesAlimenticiosController : Controller
    {
        private readonly AppDBContext _context;
        private readonly ILogger<PlanesAlimenticiosController> _logger;

        public PlanesAlimenticiosController(AppDBContext context, ILogger<PlanesAlimenticiosController> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            _logger.LogInformation($"[CLIENTE] Usuario {userId} accediendo a planes alimenticios");

            var usuario = await _context.Usuarios
                .Include(u => u.Rol)
                .FirstOrDefaultAsync(u => u.UsuarioId == userId);

            if (usuario?.RolId != 1)
            {
                _logger.LogWarning($"[CLIENTE] Usuario {userId} no es cliente - RolId: {usuario?.RolId}");
                TempData["Error"] = "Esta sección es solo para clientes.";
                return RedirectToAction("Index", "GestionPlanesAlimenticios");
            }

            var tieneAcceso = await VerificarAccesoPlanAlimenticio(userId);
            if (!tieneAcceso)
            {
                _logger.LogWarning($"[CLIENTE] Usuario {userId} sin acceso a planes alimenticios");
                TempData["Error"] = "Tu membresía actual no incluye planes alimenticios.";
                return RedirectToAction("Index", "MiMembresia");
            }

            var planes = await _context.PlanesAlimenticios
                .Include(p => p.Empleado)
                .Include(p => p.Comidas)
                .Where(p => p.ClienteId == userId)
                .OrderByDescending(p => p.FechaCreacion)
                .ToListAsync();

            _logger.LogInformation($"[CLIENTE] Usuario {userId} tiene {planes.Count} planes disponibles");

            return View(planes);
        }

        /// <summary>
        /// ? MEJORADO: Ver detalles del plan (CLIENTE) con logging completo
        /// </summary>
        public async Task<IActionResult> Detalles(int id)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            _logger.LogInformation($"[CLIENTE-DETALLES] Usuario {userId} accediendo a plan {id}");

            // ? Verificar acceso a planes alimenticios
            var tieneAcceso = await VerificarAccesoPlanAlimenticio(userId);
            if (!tieneAcceso)
            {
                _logger.LogWarning($"[CLIENTE-DETALLES] Usuario {userId} sin membresía con plan alimenticio");
                TempData["Error"] = "Tu membresía actual no incluye acceso a planes alimenticios. Actualiza tu membresía para continuar.";
                return RedirectToAction("Index", "MiMembresia");
            }

            // ? CRÍTICO: Verificar que el plan pertenezca al CLIENTE actual (no al empleado)
            var plan = await _context.PlanesAlimenticios
                .Include(p => p.Empleado)
                .Include(p => p.Comidas)
                .FirstOrDefaultAsync(p => p.PlanAlimenticioId == id && p.ClienteId == userId);

            if (plan == null)
            {
                _logger.LogWarning($"[CLIENTE-DETALLES] Plan {id} no encontrado para usuario {userId}");

                // Verificar si el plan existe pero no pertenece al usuario
                var planExiste = await _context.PlanesAlimenticios.AnyAsync(p => p.PlanAlimenticioId == id);
                if (planExiste)
                {
                    _logger.LogWarning($"[CLIENTE-DETALLES] Plan {id} existe pero no pertenece al usuario {userId} - Acceso denegado");
                    TempData["Error"] = "No tienes permiso para ver este plan alimenticio.";
                }
                else
                {
                    _logger.LogWarning($"[CLIENTE-DETALLES] Plan {id} no existe en la base de datos");
                    TempData["Error"] = "Plan alimenticio no encontrado.";
                }

                return RedirectToAction(nameof(Index));
            }

            _logger.LogInformation($"[CLIENTE-DETALLES] Plan {id} cargado exitosamente - {plan.Comidas?.Count ?? 0} comidas - Cliente: {userId}");

            return View(plan);
        }

        private async Task<bool> VerificarAccesoPlanAlimenticio(int usuarioId)
        {
            var now = DateTime.UtcNow;
            var tieneAcceso = await _context.MembresiasUsuarios
                .Include(m => m.Plan)
                .AnyAsync(m => m.UsuarioId == usuarioId &&
                              m.Activa &&
                              m.FechaInicio <= now &&
                              m.FechaFin >= now &&
                              m.Plan.PermiteAlimentacion);

            _logger.LogInformation($"[VERIFICACIÓN-ACCESO] Usuario {usuarioId} - Tiene acceso: {tieneAcceso}");

            if (!tieneAcceso)
            {
                // Log adicional para debugging
                var membresias = await _context.MembresiasUsuarios
                    .Include(m => m.Plan)
                    .Where(m => m.UsuarioId == usuarioId)
                    .Select(m => new {
                        m.Activa,
                        m.FechaInicio,
                        m.FechaFin,
                        m.Plan.Nombre,
                        m.Plan.PermiteAlimentacion
                    })
                    .ToListAsync();

                _logger.LogWarning($"[VERIFICACIÓN-ACCESO] Usuario {usuarioId} tiene {membresias.Count} membresías pero ninguna con plan alimenticio activo");

                foreach (var m in membresias)
                {
                    _logger.LogInformation($"[VERIFICACIÓN-ACCESO] Membresía: {m.Nombre} - Activa: {m.Activa} - Alimentación: {m.PermiteAlimentacion}");
                }
            }

            return tieneAcceso;
        }
    }

    [Authorize(Roles = "Gymbro")]
    public class GestionPlanesAlimenticiosController : Controller
    {
        private readonly AppDBContext _context;
        private readonly ILogger<GestionPlanesAlimenticiosController> _logger;

        public GestionPlanesAlimenticiosController(AppDBContext context, ILogger<GestionPlanesAlimenticiosController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
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

            var planes = await _context.PlanesAlimenticios
                .Include(p => p.Cliente)
                .Include(p => p.Comidas)
                .Where(p => p.EmpleadoId == empleadoId)
                .OrderByDescending(p => p.FechaCreacion)
                .ToListAsync();

            ViewData["EmpleadoNombre"] = empleado.Nombre;
            return View(planes);
        }

        [HttpGet]
        public async Task<IActionResult> SeleccionarCliente()
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

            var clientesEnGrupo = await _context.EvaluacionesRendimiento
                .Where(e => e.EmpleadoId == empleadoId)
                .Select(e => e.ClienteId)
                .Distinct()
                .ToListAsync();

            if (!clientesEnGrupo.Any())
            {
                TempData["Warning"] = "No tienes clientes evaluados aún.";
                ViewData["EmpleadoNombre"] = empleado.Nombre;
                ViewData["TotalClientes"] = 0;
                ViewData["TotalEnGrupo"] = 0;
                ViewData["ClientesSinPlanAlimenticio"] = new List<int>();
                return View(new List<Usuario>());
            }

            var todosLosClientes = await _context.Usuarios
                .Where(u => clientesEnGrupo.Contains(u.UsuarioId))
                .OrderBy(u => u.Nombre)
                .ToListAsync();

            var now = DateTime.UtcNow;
            var clientesConPlanAlimenticio = await _context.MembresiasUsuarios
                .Include(m => m.Plan)
                .Where(m => clientesEnGrupo.Contains(m.UsuarioId) &&
                           m.Activa &&
                           m.FechaInicio <= now &&
                           m.FechaFin >= now &&
                           m.Plan.PermiteAlimentacion)
                .Select(m => m.UsuarioId)
                .Distinct()
                .ToListAsync();

            var clientesSinPlanAlimenticio = clientesEnGrupo
                .Where(id => !clientesConPlanAlimenticio.Contains(id))
                .ToList();

            ViewData["EmpleadoNombre"] = empleado.Nombre;
            ViewData["TotalClientes"] = clientesConPlanAlimenticio.Count;
            ViewData["TotalEnGrupo"] = clientesEnGrupo.Count;
            ViewData["ClientesSinPlanAlimenticio"] = clientesSinPlanAlimenticio;

            return View(todosLosClientes);
        }

        [HttpGet]
        public async Task<IActionResult> Crear(int clienteId)
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

            var clienteEnGrupo = await _context.EvaluacionesRendimiento
                .AnyAsync(e => e.ClienteId == clienteId && e.EmpleadoId == empleadoId);

            if (!clienteEnGrupo)
            {
                TempData["Error"] = "Este cliente no está en tu grupo.";
                return RedirectToAction(nameof(SeleccionarCliente));
            }

            var cliente = await _context.Usuarios.FindAsync(clienteId);

            if (cliente == null)
            {
                TempData["Error"] = "Cliente no encontrado.";
                return RedirectToAction(nameof(SeleccionarCliente));
            }

            var now = DateTime.UtcNow;
            var clienteTienePlanAlimenticio = await _context.MembresiasUsuarios
                .Include(m => m.Plan)
                .AnyAsync(m => m.UsuarioId == clienteId &&
                              m.Activa &&
                              m.FechaInicio <= now &&
                              m.FechaFin >= now &&
                              m.Plan.PermiteAlimentacion);

            if (!clienteTienePlanAlimenticio)
            {
                TempData["Error"] = $"{cliente.Nombre} no tiene membresía con plan alimenticio activo.";
                return RedirectToAction(nameof(SeleccionarCliente));
            }

            var evaluacion = await _context.EvaluacionesRendimiento
                .Where(e => e.ClienteId == clienteId && e.EmpleadoId == empleadoId)
                .OrderByDescending(e => e.FechaEvaluacion)
                .FirstOrDefaultAsync();

            ViewData["Cliente"] = cliente;
            ViewData["Evaluacion"] = evaluacion;
            ViewData["EmpleadoNombre"] = empleado.Nombre;

            var plan = new PlanAlimenticio
            {
                ClienteId = clienteId,
                EmpleadoId = empleadoId,
                Nombre = $"Plan Alimenticio - {cliente.Nombre}",
                Objetivo = evaluacion?.ObjetivoCliente ?? "Mantener peso saludable",
                FechaCreacion = DateTime.Now
            };

            return View(plan);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Crear(PlanAlimenticio model)
        {
            try
            {
                var empleadoId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

                _logger.LogInformation($"[PLAN] Creando plan - EmpleadoId: {empleadoId}, ClienteId: {model.ClienteId}");

                // Validaciones
                if (string.IsNullOrWhiteSpace(model.Nombre))
                {
                    ModelState.AddModelError("Nombre", "El nombre del plan es obligatorio");
                }

                if (string.IsNullOrWhiteSpace(model.Objetivo))
                {
                    ModelState.AddModelError("Objetivo", "El objetivo del plan es obligatorio");
                }

                model.EmpleadoId = empleadoId;
                model.FechaCreacion = DateTime.Now;

                // ? SOLUCIÓN: Limpiar y recrear la colección SIEMPRE
                model.Comidas = new List<Comida>();

                // Procesar comidas desde el formulario
                var comidasKeys = Request.Form.Keys
                    .Where(k => k.StartsWith("comidas[") && k.EndsWith("].Nombre"))
                    .ToList();

                _logger.LogInformation($"[PLAN] Procesando {comidasKeys.Count} comidas");

                // ? NUEVO: Usar HashSet para detectar duplicados
                var nombresUnicos = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                foreach (var key in comidasKeys)
                {
                    var indexStr = key.Replace("comidas[", "").Replace("].Nombre", "");

                    if (int.TryParse(indexStr, out int index))
                    {
                        var nombre = Request.Form[$"comidas[{index}].Nombre"].ToString().Trim();

                        if (!string.IsNullOrWhiteSpace(nombre))
                        {
                            // ? Validar duplicados usando HashSet
                            if (nombresUnicos.Contains(nombre))
                            {
                                TempData["Error"] = $"?? La comida '{nombre}' aparece más de una vez. Cada comida debe tener un nombre único.";
                                var cliente = await _context.Usuarios.FindAsync(model.ClienteId);
                                var empleado = await _context.Usuarios.FindAsync(empleadoId);
                                var evaluacion = await _context.EvaluacionesRendimiento
                                    .Where(e => e.ClienteId == model.ClienteId && e.EmpleadoId == empleadoId)
                                    .OrderByDescending(e => e.FechaEvaluacion)
                                    .FirstOrDefaultAsync();

                                ViewData["Cliente"] = cliente;
                                ViewData["Evaluacion"] = evaluacion;
                                ViewData["EmpleadoNombre"] = empleado?.Nombre;
                                return View(model);
                            }

                            nombresUnicos.Add(nombre);

                            var ingredientes = Request.Form[$"comidas[{index}].Ingredientes"].ToString().Trim();
                            var horarios = Request.Form[$"comidas[{index}].Horarios"].ToString().Trim();

                            model.Comidas.Add(new Comida
                            {
                                Nombre = nombre,
                                Ingredientes = ingredientes,
                                Horarios = horarios
                            });

                            _logger.LogInformation($"[PLAN] Comida agregada: {nombre}");
                        }
                    }
                }

                if (model.Comidas.Count == 0)
                {
                    ModelState.AddModelError("", "Debes agregar al menos una comida.");
                }

                if (!ModelState.IsValid)
                {
                    var cliente = await _context.Usuarios.FindAsync(model.ClienteId);
                    var empleado = await _context.Usuarios.FindAsync(empleadoId);
                    var evaluacion = await _context.EvaluacionesRendimiento
                        .Where(e => e.ClienteId == model.ClienteId && e.EmpleadoId == empleadoId)
                        .OrderByDescending(e => e.FechaEvaluacion)
                        .FirstOrDefaultAsync();

                    ViewData["Cliente"] = cliente;
                    ViewData["Evaluacion"] = evaluacion;
                    ViewData["EmpleadoNombre"] = empleado?.Nombre;
                    TempData["Error"] = "Hay errores en el formulario.";
                    return View(model);
                }

                // ? CRÍTICO: Desconectar cualquier rastreo previo
                _context.ChangeTracker.Clear();

                // Agregar el plan al contexto
                _context.PlanesAlimenticios.Add(model);

                // ? UN SOLO SaveChangesAsync - EF Core rastrea automáticamente las comidas
                await _context.SaveChangesAsync();

                _logger.LogInformation($"[PLAN] ? Plan {model.PlanAlimenticioId} creado con {model.Comidas.Count} comidas");

                TempData["Success"] = $"? Plan '{model.Nombre}' creado con {model.Comidas.Count} comida(s).";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[PLAN] Error al crear");
                TempData["Error"] = $"Error: {ex.Message}";

                var cliente = await _context.Usuarios.FindAsync(model.ClienteId);
                var empleado = await _context.Usuarios.FindAsync(model.EmpleadoId);
                var evaluacion = await _context.EvaluacionesRendimiento
                    .Where(e => e.ClienteId == model.ClienteId && e.EmpleadoId == model.EmpleadoId)
                    .OrderByDescending(e => e.FechaEvaluacion)
                    .FirstOrDefaultAsync();

                ViewData["Cliente"] = cliente;
                ViewData["Evaluacion"] = evaluacion;
                ViewData["EmpleadoNombre"] = empleado?.Nombre;

                return View(model);
            }
        }

        [HttpGet]
        public async Task<IActionResult> Editar(int id)
        {
            var empleadoId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            var plan = await _context.PlanesAlimenticios
                .Include(p => p.Cliente)
                .Include(p => p.Comidas)
                .FirstOrDefaultAsync(p => p.PlanAlimenticioId == id && p.EmpleadoId == empleadoId);

            if (plan == null)
            {
                TempData["Error"] = "Plan no encontrado.";
                return RedirectToAction(nameof(Index));
            }

            var empleado = await _context.Usuarios.FindAsync(empleadoId);
            var evaluacion = await _context.EvaluacionesRendimiento
                .Where(e => e.ClienteId == plan.ClienteId && e.EmpleadoId == empleadoId)
                .OrderByDescending(e => e.FechaEvaluacion)
                .FirstOrDefaultAsync();

            ViewData["Cliente"] = plan.Cliente;
            ViewData["Evaluacion"] = evaluacion;
            ViewData["EmpleadoNombre"] = empleado?.Nombre;

            return View(plan);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Editar(PlanAlimenticio model)
        {
            try
            {
                var empleadoId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

                var planExistente = await _context.PlanesAlimenticios
                    .Include(p => p.Comidas)
                    .FirstOrDefaultAsync(p => p.PlanAlimenticioId == model.PlanAlimenticioId && p.EmpleadoId == empleadoId);

                if (planExistente == null)
                {
                    TempData["Error"] = "Plan no encontrado.";
                    return RedirectToAction(nameof(Index));
                }

                // Actualizar datos básicos
                planExistente.Nombre = model.Nombre;
                planExistente.Objetivo = model.Objetivo;

                // Eliminar comidas antiguas
                if (planExistente.Comidas != null && planExistente.Comidas.Any())
                {
                    _context.Comidas.RemoveRange(planExistente.Comidas);
                }

                // Procesar comidas nuevas
                var comidas = new List<Comida>();
                var comidasKeys = Request.Form.Keys
                    .Where(k => k.StartsWith("comidas[") && k.EndsWith("].Nombre"))
                    .ToList();

                foreach (var key in comidasKeys)
                {
                    var indexStr = key.Replace("comidas[", "").Replace("].Nombre", "");

                    if (int.TryParse(indexStr, out int index))
                    {
                        var nombre = Request.Form[$"comidas[{index}].Nombre"].ToString().Trim();
                        var ingredientes = Request.Form[$"comidas[{index}].Ingredientes"].ToString().Trim();
                        var horarios = Request.Form[$"comidas[{index}].Horarios"].ToString().Trim();

                        if (!string.IsNullOrWhiteSpace(nombre))
                        {
                            comidas.Add(new Comida
                            {
                                PlanAlimenticioId = planExistente.PlanAlimenticioId,
                                Nombre = nombre,
                                Ingredientes = ingredientes,
                                Horarios = horarios
                            });
                        }
                    }
                }

                if (comidas.Any())
                {
                    _context.Comidas.AddRange(comidas);
                }

                await _context.SaveChangesAsync();

                TempData["Success"] = "? Plan actualizado correctamente.";
                return RedirectToAction(nameof(Detalles), new { id = planExistente.PlanAlimenticioId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al editar plan");
                TempData["Error"] = $"Error: {ex.Message}";
                return View(model);
            }
        }

        [HttpGet]
        public async Task<IActionResult> Detalles(int id)
        {
            var empleadoId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            var plan = await _context.PlanesAlimenticios
                .Include(p => p.Cliente)
                .Include(p => p.Empleado)
                .Include(p => p.Comidas)
                .FirstOrDefaultAsync(p => p.PlanAlimenticioId == id && p.EmpleadoId == empleadoId);

            if (plan == null)
            {
                TempData["Error"] = "Plan no encontrado.";
                return RedirectToAction(nameof(Index));
            }

            var empleado = await _context.Usuarios.FindAsync(empleadoId);
            ViewData["EmpleadoNombre"] = empleado?.Nombre;

            return View(plan);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Eliminar(int id)
        {
            var empleadoId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            var plan = await _context.PlanesAlimenticios
                .Include(p => p.Comidas)
                .FirstOrDefaultAsync(p => p.PlanAlimenticioId == id && p.EmpleadoId == empleadoId);

            if (plan == null)
            {
                TempData["Error"] = "Plan no encontrado.";
                return RedirectToAction(nameof(Index));
            }

            // Eliminar comidas asociadas
            if (plan.Comidas != null && plan.Comidas.Any())
            {
                _context.Comidas.RemoveRange(plan.Comidas);
            }

            // Eliminar plan
            _context.PlanesAlimenticios.Remove(plan);

            // Un solo SaveChangesAsync
            await _context.SaveChangesAsync();

            TempData["Success"] = "? Plan eliminado correctamente.";
            return RedirectToAction(nameof(Index));
        }

        /// <summary>
        /// ? NUEVO: Eliminar comida individual
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EliminarComida(int id, int planId)
        {
            try
            {
                var empleadoId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

                var plan = await _context.PlanesAlimenticios
                    .FirstOrDefaultAsync(p => p.PlanAlimenticioId == planId && p.EmpleadoId == empleadoId);

                if (plan == null)
                {
                    TempData["Error"] = "Plan no encontrado.";
                    return RedirectToAction(nameof(Index));
                }

                var comida = await _context.Comidas
                    .FirstOrDefaultAsync(c => c.ComidaId == id && c.PlanAlimenticioId == planId);

                if (comida == null)
                {
                    TempData["Error"] = "Comida no encontrada.";
                    return RedirectToAction(nameof(Detalles), new { id = planId });
                }

                _context.Comidas.Remove(comida);
                await _context.SaveChangesAsync();

                TempData["Success"] = $"? Comida '{comida.Nombre}' eliminada.";
                return RedirectToAction(nameof(Detalles), new { id = planId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al eliminar comida");
                TempData["Error"] = $"Error: {ex.Message}";
                return RedirectToAction(nameof(Detalles), new { id = planId });
            }
        }
    }
}