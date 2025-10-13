using System.ComponentModel.DataAnnotations;

namespace GYM.ViewModels
{
    public class UsuarioCreateVM
    {
        [Required] public string Nombre { get; set; } = string.Empty;
        [Required, EmailAddress] public string Email { get; set; } = string.Empty;
        [Required, StringLength(8, ErrorMessage = "Máx. 8 caracteres")] public string Password { get; set; } = string.Empty;
        public string? Telefono { get; set; }
        [Required] public int RolId { get; set; } // 1 o 2
    }

    // ViewModels/UsuarioEditVM.cs
}
