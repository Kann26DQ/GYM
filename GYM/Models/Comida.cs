using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GYM.Models
{
    public class Comida
    {
        [Key]
        public int ComidaId { get; set; }

        [Required(ErrorMessage = "El nombre de la comida es obligatorio")]
        [StringLength(100, ErrorMessage = "El nombre no puede exceder {1} caracteres")]
        public string Nombre { get; set; } = string.Empty;

        [Required(ErrorMessage = "Los ingredientes son obligatorios")]
        [StringLength(2000, ErrorMessage = "Los ingredientes no pueden exceder {1} caracteres")]
        public string Ingredientes { get; set; } = string.Empty;

        [Required(ErrorMessage = "El horario es obligatorio")]
        [StringLength(200, ErrorMessage = "El horario no puede exceder {1} caracteres")]
        public string Horarios { get; set; } = string.Empty;

        // ✅ CORRECCIÓN: Relación correcta con PlanAlimenticio
        [Required]
        public int PlanAlimenticioId { get; set; }

        [ForeignKey(nameof(PlanAlimenticioId))]
        public virtual PlanAlimenticio? PlanAlimenticio { get; set; }
    }
}