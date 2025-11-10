using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;

namespace GYM.ViewModels
{
    public class UsuarioEditVM
    {
        [Required]
        public int UsuarioId { get; set; }

        [Required(ErrorMessage = "El nombre es obligatorio")]
        public string Nombre { get; set; } = string.Empty;

        [Required(ErrorMessage = "El correo es obligatorio")]
        [EmailAddress(ErrorMessage = "El formato del correo no es válido")]
        [RegularExpression(@"^[^@\s]+@[^@\s]+\.com$", ErrorMessage = "El correo debe ser válido y terminar en .com")]
        [Remote(action: "CheckEmailUnique", controller: "GestionUsuarios", AdditionalFields = nameof(UsuarioId), ErrorMessage = "Este correo ya está registrado")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "El teléfono es obligatorio")]
        [RegularExpression(@"^\d{9}$", ErrorMessage = "El teléfono debe tener exactamente 9 dígitos")]
        [Remote(action: "CheckTelefonoUnique", controller: "GestionUsuarios", AdditionalFields = nameof(UsuarioId), ErrorMessage = "Este número de teléfono ya está registrado")]
        public string? Telefono { get; set; }

        [Required(ErrorMessage = "Debe seleccionar un rol")]
        public int RolId { get; set; }

        public bool Activo { get; set; }

        // Opcional: cambiar contraseña
        [MinLength(8, ErrorMessage = "La nueva contraseña debe tener al menos 8 caracteres")]
        public string? NewPassword { get; set; }
    }
}