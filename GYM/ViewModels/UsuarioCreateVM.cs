using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;

namespace GYM.ViewModels
{
    public class UsuarioCreateVM
    {
        [Required(ErrorMessage = "El nombre es obligatorio")]
        public string Nombre { get; set; } = string.Empty;

        [Required(ErrorMessage = "El correo es obligatorio")]
        [EmailAddress(ErrorMessage = "El formato del correo no es válido")]
        [RegularExpression(@"^[^@\s]+@[^@\s]+\.com$", ErrorMessage = "El correo debe ser válido y terminar en .com")]
        [Remote(action: "CheckEmailUnique", controller: "GestionUsuarios", ErrorMessage = "Este correo ya está registrado")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "La contraseña es obligatoria")]
        [MinLength(8, ErrorMessage = "La contraseña debe tener al menos 8 caracteres")]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "Debe confirmar su contraseña")]
        [Compare("Password", ErrorMessage = "Las contraseñas no coinciden")]
        public string ConfirmarPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "El teléfono es obligatorio")]
        [RegularExpression(@"^\d{9}$", ErrorMessage = "El teléfono debe tener exactamente 9 dígitos")]
        [Remote(action: "CheckTelefonoUnique", controller: "GestionUsuarios", ErrorMessage = "Este número de teléfono ya está registrado")]
        public string? Telefono { get; set; }

        [Required(ErrorMessage = "Debe seleccionar un rol")]
        public int RolId { get; set; }

        // 👇 NUEVO: Membresía opcional
        [Display(Name = "Membresía (Opcional)")]
        public int? MembresiaPlanId { get; set; }
    }
}