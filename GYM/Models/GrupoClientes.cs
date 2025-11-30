using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GYM.Models
{
    public class GrupoClientes
    {
        [Key]
        public int GrupoClientesId { get; set; }

        [Required(ErrorMessage = "El título del grupo es obligatorio")]
        [StringLength(100, ErrorMessage = "El título no puede exceder 100 caracteres")]
        [Display(Name = "Título del Grupo")]
        public string Titulo { get; set; } = string.Empty;

        [Required(ErrorMessage = "El color es obligatorio")]
        [StringLength(7, MinimumLength = 7, ErrorMessage = "El color debe estar en formato hexadecimal (#RRGGBB)")]
        [Display(Name = "Color del Grupo")]
        [RegularExpression(@"^#[0-9A-Fa-f]{6}$", ErrorMessage = "El color debe estar en formato hexadecimal válido (#RRGGBB)")]
        public string Color { get; set; } = "#ffc107"; // Amarillo por defecto

        [Required]
        public int EmpleadoId { get; set; }

        [Required]
        public DateTime FechaCreacion { get; set; } = DateTime.Now;

        public bool Activo { get; set; } = true;

        // Relaciones
        [ForeignKey(nameof(EmpleadoId))]
        public virtual Usuario? Empleado { get; set; }

        public virtual ICollection<EvaluacionRendimiento>? Evaluaciones { get; set; }
    }
}