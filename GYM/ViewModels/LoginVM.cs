using System.ComponentModel.DataAnnotations;

namespace GYM.ViewModels
{
    public class LoginVM
    {
        [Required(ErrorMessage = "El correo es obligatorio")]
        [EmailAddress(ErrorMessage = "El formato del correo no es válido")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "La contraseña es obligatoria")]
        [MinLength(8, ErrorMessage = "La contraseña debe tener al menos 8 caracteres")]
        [RegularExpression(@"^\S+$", ErrorMessage = "La contraseña no puede contener espacios")]
        public string password { get; set; } = string.Empty;
    }
}