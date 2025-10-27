using GYM.Data;
using GYM.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GYM.ViewComponents
{
    public class HistorialComprasViewComponent : ViewComponent
    {
        private readonly AppDBContext _context;

        public HistorialComprasViewComponent(AppDBContext context)
        {
            _context = context;
        }

        public async Task<IViewComponentResult> InvokeAsync(int max = 5)
        {
            var userId = GetCurrentUserId();
            if (userId == 0 || !HttpContext.User.Identity?.IsAuthenticated == true)
                return View(new List<Venta>());

            var ventas = await _context.Ventas
                .AsNoTracking()
                .Where(v => v.ClienteId == userId || (v.EmpleadoId != null && v.EmpleadoId == userId))
                .OrderByDescending(v => v.Fecha)
                .Take(max)
                .ToListAsync();

            return View(ventas);
        }

        private int GetCurrentUserId()
        {
            var userIdClaim = HttpContext.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(userIdClaim, out var userId) ? userId : 0;
        }
    }
}