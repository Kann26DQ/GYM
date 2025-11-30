using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GYM.Models
{
    public class PlanAlimenticio
    {
        [Key]
        public int PlanAlimenticioId { get; set; }

        [Required(ErrorMessage = "El nombre del plan es obligatorio")]
        [StringLength(100, ErrorMessage = "El nombre no puede exceder {1} caracteres")]
        public string Nombre { get; set; } = string.Empty;

        [Required(ErrorMessage = "El objetivo es obligatorio")]
        [StringLength(500, ErrorMessage = "El objetivo no puede exceder {1} caracteres")]
        public string Objetivo { get; set; } = string.Empty;

        [Display(Name = "Fecha de Creación")]
        public DateTime FechaCreacion { get; set; } = DateTime.Now;

        // ✅ CAMBIO CRÍTICO: NO deben ser nullable
        [Required(ErrorMessage = "El cliente es obligatorio")]
        public int ClienteId { get; set; }

        [ForeignKey(nameof(ClienteId))]
        public virtual Usuario? Cliente { get; set; }

        [Required(ErrorMessage = "El empleado es obligatorio")]
        public int EmpleadoId { get; set; }

        [ForeignKey(nameof(EmpleadoId))]
        public virtual Usuario? Empleado { get; set; }

        public virtual ICollection<Comida> Comidas { get; set; } = new List<Comida>();
    }
}