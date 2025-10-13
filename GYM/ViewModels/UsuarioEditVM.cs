using System.ComponentModel.DataAnnotations;

namespace GYM.ViewModels
{

    public class UsuarioEditVM
    {
        public int UsuarioId { get; set; }
        [Required] public string Nombre { get; set; } = string.Empty;
        [Required, EmailAddress] public string Email { get; set; } = string.Empty;
        // Password opcional: solo si se envía se reasigna
        [StringLength(8, ErrorMessage = "Máx. 8 caracteres")]
        public string? NewPassword { get; set; }
        public string? Telefono { get; set; }
        [Required] public int RolId { get; set; }
        public bool Activo { get; set; }
    }
}
