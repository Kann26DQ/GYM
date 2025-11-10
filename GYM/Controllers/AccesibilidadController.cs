using Microsoft.AspNetCore.Mvc;

namespace GYM.Controllers
{
    /// <summary>
    /// Controlador para gestionar configuraciones de accesibilidad
    /// ODS 10: Reducción de Desigualdades - Software Inclusivo
    /// </summary>
    public class AccesibilidadController : Controller
    {
        /// <summary>
        /// Vista de configuración de accesibilidad
        /// </summary>
        [HttpGet]
        public IActionResult Configuracion()
        {
            ViewData["AltoContraste"] = Request.Cookies["AltoContraste"] == "true";
            ViewData["TextoGrande"] = Request.Cookies["TextoGrande"] == "true";
            ViewData["ReducirAnimaciones"] = Request.Cookies["ReducirAnimaciones"] == "true";

            return View();
        }

        /// <summary>
        /// Activar/Desactivar Modo Alto Contraste
        /// </summary>
        [HttpPost]
        public IActionResult ToggleAltoContraste(bool activar)
        {
            var cookieOptions = new CookieOptions
            {
                Expires = DateTimeOffset.UtcNow.AddYears(1),
                HttpOnly = false,
                IsEssential = true
            };

            Response.Cookies.Append("AltoContraste", activar.ToString().ToLower(), cookieOptions);

            return Json(new { success = true, activado = activar });
        }

        /// <summary>
        /// Activar/Desactivar Texto Grande
        /// </summary>
        [HttpPost]
        public IActionResult ToggleTextoGrande(bool activar)
        {
            var cookieOptions = new CookieOptions
            {
                Expires = DateTimeOffset.UtcNow.AddYears(1),
                HttpOnly = false,
                IsEssential = true
            };

            Response.Cookies.Append("TextoGrande", activar.ToString().ToLower(), cookieOptions);

            return Json(new { success = true, activado = activar });
        }

        /// <summary>
        /// Activar/Desactivar Reducir Animaciones
        /// </summary>
        [HttpPost]
        public IActionResult ToggleReducirAnimaciones(bool activar)
        {
            var cookieOptions = new CookieOptions
            {
                Expires = DateTimeOffset.UtcNow.AddYears(1),
                HttpOnly = false,
                IsEssential = true
            };

            Response.Cookies.Append("ReducirAnimaciones", activar.ToString().ToLower(), cookieOptions);

            return Json(new { success = true, activado = activar });
        }

        /// <summary>
        /// Restablecer todas las configuraciones de accesibilidad
        /// </summary>
        [HttpPost]
        public IActionResult Restablecer()
        {
            Response.Cookies.Delete("AltoContraste");
            Response.Cookies.Delete("TextoGrande");
            Response.Cookies.Delete("ReducirAnimaciones");

            TempData["Success"] = "Configuraciones de accesibilidad restablecidas.";
            return RedirectToAction("Configuracion");
        }
    }
}