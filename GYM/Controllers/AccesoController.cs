using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using GYM.Data;
using GYM.Models;
using GYM.ViewModels;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;

namespace GYM.Controllers
{
    public class AccesoController : Controller
    {
        private readonly AppDBContext _appDBContext;

        public AccesoController(AppDBContext appDBContext)
        {
            _appDBContext = appDBContext;
        }

        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        // POST: Login
        [HttpPost]
        public async Task<IActionResult> Login(LoginVM model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var usuario = await _appDBContext.Usuarios
                .Include(u => u.Rol)
                .FirstOrDefaultAsync(u => u.Email == model.Email);

            if (usuario == null)
            {
                ViewData["Mensaje"] = "La cuenta no existe, por favor regístrese.";
                return View(model);
            }

            if (usuario.RolId == 4 || usuario.Rol.Nombre == "Proveedor")
            {
                ViewData["Mensaje"] = "El rol 'Proveedor' no tiene acceso al sistema.";
                return View(model);
            }

            // Verificar contraseña
            var hasher = new PasswordHasher<Usuario>();
            var result = hasher.VerifyHashedPassword(usuario, usuario.Password, model.password);

            if (result == PasswordVerificationResult.Failed)
            {
                ViewData["Mensaje"] = "Contraseña incorrecta.";
                return View(model);
            }

            // Verificar si el usuario está activo (solo para Cliente y Gymbro)
            if (!usuario.Activo && (usuario.Rol.Nombre == "Cliente" || usuario.Rol.Nombre == "Gymbro"))
            {
                // 👈 En lugar de mostrar mensaje, redirigir a membresías
                // Crear sesión temporal para que pueda comprar
                HttpContext.Session.SetInt32("UsuarioId", usuario.UsuarioId);
                HttpContext.Session.SetString("Nombre", usuario.Nombre);
                HttpContext.Session.SetString("Rol", usuario.Rol.Nombre);

                // Crear claims temporales
                var claimsTemp = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, usuario.UsuarioId.ToString()),
                    new Claim(ClaimTypes.Name, usuario.Nombre),
                    new Claim(ClaimTypes.Email, usuario.Email),
                    new Claim(ClaimTypes.Role, usuario.Rol.Nombre)
                };

                var identityTemp = new ClaimsIdentity(claimsTemp, CookieAuthenticationDefaults.AuthenticationScheme);
                var principalTemp = new ClaimsPrincipal(identityTemp);

                await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principalTemp);

                TempData["Warning"] = "Tu cuenta está inactiva. Por favor, adquiere una membresía para activarla.";
                return RedirectToAction("Store", "Membresias");
            }

            // Guardamos sesión
            HttpContext.Session.SetInt32("UsuarioId", usuario.UsuarioId);
            HttpContext.Session.SetString("Nombre", usuario.Nombre);
            HttpContext.Session.SetString("Rol", usuario.Rol.Nombre);

            // Crear claims e iniciar sesión con cookie authentication
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, usuario.UsuarioId.ToString()),
                new Claim(ClaimTypes.Name, usuario.Nombre),
                new Claim(ClaimTypes.Email, usuario.Email),
                new Claim(ClaimTypes.Role, usuario.Rol.Nombre)
            };

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

            // Redirigir según rol
            if (usuario.Rol.Nombre == "SuperAdmin")
                return RedirectToAction("Index", "SuperAdmin");
            else if (usuario.Rol.Nombre == "Gymbro")
                return RedirectToAction("Index", "gymbro");
            else
                return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        // POST: Register
        [HttpPost]
        public async Task<IActionResult> Register(UsuarioVM model)
        {
            if (!ModelState.IsValid) return View(model);

            // Validación servidor: email único
            var exists = await _appDBContext.Usuarios
                .AsNoTracking()
                .AnyAsync(u => u.Email.ToLower() == model.Email.ToLower());
            if (exists)
            {
                ModelState.AddModelError(nameof(model.Email), "Este correo ya está registrado");
                return View(model);
            }

            var usuario = new Usuario
            {
                Nombre = model.Nombre,
                Email = model.Email,
                Telefono = model.Telefono,
                FechaRegistro = DateTime.Now,
                RolId = 1, // Cliente por defecto
                Activo = false // 👈 Cuenta desactivada hasta que compre membresía
            };

            // Encriptar contraseña
            var hasher = new PasswordHasher<Usuario>();
            usuario.Password = hasher.HashPassword(usuario, model.Password);

            _appDBContext.Usuarios.Add(usuario);
            await _appDBContext.SaveChangesAsync();

            TempData["RegistroExitoso"] = "Cuenta creada exitosamente. Inicia sesión para ver las membresías disponibles.";
            return RedirectToAction("Login", "Acceso");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            HttpContext.Session.Clear();
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login");
        }

        // Ruta usada por la configuración de AccessDeniedPath
        [HttpGet]
        public IActionResult AccessDenied()
        {
            return StatusCode(403, "Acceso denegado");
        }

        // Validación remota para el email
        [HttpGet]
        public async Task<IActionResult> CheckEmail(string email)
        {
            var existe = await _appDBContext.Usuarios
                .AnyAsync(u => u.Email.ToLower() == email.ToLower());
            return Json(!existe);
        }
    }
}