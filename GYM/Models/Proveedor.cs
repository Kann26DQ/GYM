using System.ComponentModel.DataAnnotations;

namespace GYM.Models
{
   public class Proveedor
{
        public int ProveedorId { get; set; }

        [Required(ErrorMessage = "El nombre es obligatorio.")]
        [StringLength(120, ErrorMessage = "Máximo {1} caracteres.")]
        public string Nombre { get; set; } = string.Empty;

        [Required(ErrorMessage = "El teléfono es obligatorio.")]
        [RegularExpression(@"^\d{9}$", ErrorMessage = "El teléfono debe tener exactamente 9 dígitos numéricos.")]
        public string Telefono { get; set; } = string.Empty;

        [Required(ErrorMessage = "El email es obligatorio.")]
        [RegularExpression(@"^[^@\s]+@[^@\s]+\.com$", ErrorMessage = "Debe ser un correo válido que termine en .com.")]
        public string Email { get; set; } = string.Empty;

        [StringLength(250, ErrorMessage = "Máximo {1} caracteres.")]
        public string? Direccion { get; set; }

        public bool Estado { get; set; } = true;


        public ICollection<Producto> Productos { get; set; }
}

}
