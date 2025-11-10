using GYM.Data;
using GYM.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GYM.Controllers.SuperAdmin
{
    [Authorize(Roles = "SuperAdmin")]
    public class MembresiaPlanesController : Controller
    {
        private readonly AppDBContext _ctx;
        public MembresiaPlanesController(AppDBContext ctx) => _ctx = ctx;

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var planes = await _ctx.MembresiaPlanes.AsNoTracking().OrderBy(p => p.Precio).ToListAsync();
            return View("~/Views/SuperAdmin/MembresiaPlanes/Index.cshtml", planes);
        }

        [HttpGet]
        public IActionResult Create() => View("~/Views/SuperAdmin/MembresiaPlanes/Create.cshtml");

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(MembresiaPlan plan)
        {
            if (!ModelState.IsValid) return View("~/Views/SuperAdmin/MembresiaPlanes/Create.cshtml", plan);

            // Verificar si ya existe una membresía con el mismo nombre
            var existe = await _ctx.MembresiaPlanes
                .AnyAsync(m => m.Nombre.ToLower() == plan.Nombre.ToLower());

            if (existe)
            {
                ModelState.AddModelError("Nombre", "Ya existe una membresía con este nombre.");
                return View("~/Views/SuperAdmin/MembresiaPlanes/Create.cshtml", plan);
            }

            _ctx.MembresiaPlanes.Add(plan);
            await _ctx.SaveChangesAsync();
            TempData["Success"] = "Membresía creada correctamente.";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var plan = await _ctx.MembresiaPlanes.FindAsync(id);
            if (plan == null) return NotFound();
            return View("~/Views/SuperAdmin/MembresiaPlanes/Edit.cshtml", plan);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(MembresiaPlan plan)
        {
            if (!ModelState.IsValid) return View("~/Views/SuperAdmin/MembresiaPlanes/Edit.cshtml", plan);

            // Verificar si existe otra membresía con el mismo nombre (excluyendo la actual)
            var existe = await _ctx.MembresiaPlanes
                .AnyAsync(m => m.Nombre.ToLower() == plan.Nombre.ToLower() && m.MembresiaPlanId != plan.MembresiaPlanId);

            if (existe)
            {
                ModelState.AddModelError("Nombre", "Ya existe otra membresía con este nombre.");
                return View("~/Views/SuperAdmin/MembresiaPlanes/Edit.cshtml", plan);
            }

            _ctx.MembresiaPlanes.Update(plan);
            await _ctx.SaveChangesAsync();
            TempData["Success"] = "Membresía actualizada correctamente.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Toggle(int id)
        {
            var plan = await _ctx.MembresiaPlanes.FindAsync(id);
            if (plan == null) return NotFound();
            plan.Activo = !plan.Activo;
            await _ctx.SaveChangesAsync();
            TempData["Success"] = $"Membresía {(plan.Activo ? "activada" : "desactivada")} correctamente.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var plan = await _ctx.MembresiaPlanes.FindAsync(id);
            if (plan == null) return NotFound();

            // Verificar si hay usuarios con esta membresía
            var tieneUsuarios = await _ctx.MembresiasUsuarios
                .AnyAsync(mu => mu.MembresiaPlanId == id);

            if (tieneUsuarios)
            {
                TempData["Error"] = "No se puede eliminar la membresía porque tiene usuarios asociados.";
                return RedirectToAction(nameof(Index));
            }

            _ctx.MembresiaPlanes.Remove(plan);
            await _ctx.SaveChangesAsync();
            TempData["Success"] = "Membresía eliminada correctamente.";
            return RedirectToAction(nameof(Index));
        }
    }
}