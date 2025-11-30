using GYM.Data;
using GYM.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GYM.Controllers.SuperAdmin
{
    [Authorize(Roles = "SuperAdmin")]
    public class VentasController : Controller
    {
        private readonly AppDBContext _context;

        public VentasController(AppDBContext context)
        {
            _context = context;
        }

        /// <summary>
        /// ✅ NUEVO: Index con filtro de búsqueda
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Index(string buscar, string tipoFiltro = "cliente", DateTime? fechaDesde = null, DateTime? fechaHasta = null)
        {
            var query = _context.Ventas
                .Include(v => v.Cliente)
                .Include(v => v.Detalles)
                .ThenInclude(d => d.Producto)
                .AsQueryable();

            // ✅ Filtro por fechas
            if (fechaDesde.HasValue)
            {
                query = query.Where(v => v.Fecha >= fechaDesde.Value);
            }

            if (fechaHasta.HasValue)
            {
                var fechaHastaFin = fechaHasta.Value.Date.AddDays(1).AddSeconds(-1);
                query = query.Where(v => v.Fecha <= fechaHastaFin);
            }

            // ✅ Aplicar filtros de búsqueda por texto
            if (!string.IsNullOrWhiteSpace(buscar))
            {
                buscar = buscar.Trim();

                switch (tipoFiltro.ToLower())
                {
                    case "cliente":
                        query = query.Where(v => v.Cliente != null && v.Cliente.Nombre.ToLower().Contains(buscar.ToLower()));
                        break;
                    case "ventaid":
                        if (int.TryParse(buscar, out int ventaId) && ventaId > 0)
                        {
                            query = query.Where(v => v.VentaId == ventaId);
                        }
                        break;
                    case "total":
                        if (decimal.TryParse(buscar, out decimal total) && total >= 0)
                        {
                            query = query.Where(v => v.Total == total);
                        }
                        break;
                    default:
                        // Búsqueda general
                        query = query.Where(v =>
                            (v.Cliente != null && v.Cliente.Nombre.ToLower().Contains(buscar.ToLower())) ||
                            v.VentaId.ToString().Contains(buscar));
                        break;
                }
            }

            var ventas = await query
                .OrderByDescending(v => v.Fecha)
                .ToListAsync();

            // Calcular estadísticas
            ViewBag.TotalVentas = ventas.Sum(v => v.Total);
            ViewBag.CantidadVentas = ventas.Count;
            ViewBag.PromedioVenta = ventas.Any() ? ventas.Average(v => v.Total) : 0;

            // Pasar valores al ViewBag para mantenerlos en la vista
            ViewBag.BuscarActual = buscar;
            ViewBag.TipoFiltroActual = tipoFiltro;
            ViewBag.FechaDesde = fechaDesde;
            ViewBag.FechaHasta = fechaHasta;

            return View("~/Views/SuperAdmin/Ventas/Index.cshtml", ventas);
        }

        // ------------------------------
        // VER DETALLES DE UNA VENTA
        // ------------------------------
        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var venta = await _context.Ventas
                .Include(v => v.Cliente)
                .Include(v => v.Empleado)
                .Include(v => v.Detalles)
                .ThenInclude(d => d.Producto)
                .FirstOrDefaultAsync(v => v.VentaId == id);

            if (venta == null)
                return NotFound();

            return View("~/Views/SuperAdmin/Ventas/Details.cshtml", venta);
        }
    }
}