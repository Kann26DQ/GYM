using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;

namespace GYM.ViewModels
{
    public class UsuarioVM
    {
        [Required(ErrorMessage = "El nombre es obligatorio")]
        public string Nombre { get; set; } = string.Empty;

        [Required(ErrorMessage = "El correo es obligatorio")]
        [EmailAddress(ErrorMessage = "El formato del correo no es válido")]
        [RegularExpression(@"^[^@\s]+@[^@\s]+\.com$", ErrorMessage = "El correo debe ser válido y terminar en .com")]
        [Remote(action: "CheckEmail", controller: "Acceso", ErrorMessage = "Este correo ya está registrado")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "La contraseña es obligatoria")]
        [MinLength(8, ErrorMessage = "La contraseña debe tener al menos 8 caracteres")]
        [RegularExpression(@"^\S+$", ErrorMessage = "La contraseña no puede contener espacios")]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "Debe confirmar su contraseña")]
        [Compare("Password", ErrorMessage = "Las contraseñas no coinciden")]
        public string ConfirmarPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "El teléfono es obligatorio")]
        [RegularExpression(@"^\d{9}$", ErrorMessage = "El teléfono debe tener exactamente 9 dígitos")]
        public string? Telefono { get; set; }
    }
}