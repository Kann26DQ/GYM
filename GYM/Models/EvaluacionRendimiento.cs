using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GYM.Models
{
    public class EvaluacionRendimiento
    {
        [Key]
        public int EvaluacionRendimientoId { get; set; }

        [Required]
        public int ClienteId { get; set; }

        [Required]
        public int EmpleadoId { get; set; }

        public int? GrupoClientesId { get; set; }

        [Required]
        public DateTime FechaEvaluacion { get; set; }

        [Required]
        [Range(1, 10, ErrorMessage = "La fuerza debe estar entre 1 y 10")]
        public int Fuerza { get; set; }

        [Required]
        [Range(1, 10, ErrorMessage = "La resistencia debe estar entre 1 y 10")]
        public int Resistencia { get; set; }

        [Required]
        [Range(1, 10, ErrorMessage = "La flexibilidad debe estar entre 1 y 10")]
        public int Flexibilidad { get; set; }

        [Required]
        [Range(1, 10, ErrorMessage = "La técnica debe estar entre 1 y 10")]
        public int Tecnica { get; set; }

        [Required]
        [Range(1, 10, ErrorMessage = "El nivel general debe estar entre 1 y 10")]
        public int NivelGeneral { get; set; }

        // ✅ ACTUALIZADO: Peso mínimo 40 kg, sin límite superior
        [Column(TypeName = "decimal(18,2)")]
        [Range(40, double.MaxValue, ErrorMessage = "El peso debe ser mayor a 40 kg")]
        public decimal? Peso { get; set; }

        // ✅ ACTUALIZADO: Altura mínima 140 cm (1.40 m), sin límite superior
        [Column(TypeName = "decimal(18,2)")]
        [Range(140, double.MaxValue, ErrorMessage = "La altura recomendada es mayor a 140 cm")]
        public decimal? Altura { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? IMC { get; set; }

        [MaxLength(1000)]
        public string? Observaciones { get; set; }

        [MaxLength(500)]
        public string? ObjetivoCliente { get; set; }

        // Propiedades de navegación
        [ForeignKey(nameof(ClienteId))]
        public virtual Usuario? Cliente { get; set; }

        [ForeignKey(nameof(EmpleadoId))]
        public virtual Usuario? Empleado { get; set; }

        [ForeignKey(nameof(GrupoClientesId))]
        public virtual GrupoClientes? Grupo { get; set; }

        public virtual ICollection<Rutina>? Rutinas { get; set; }

        [NotMapped]
        public double PromedioRendimiento =>
            (Fuerza + Resistencia + Flexibilidad + Tecnica + NivelGeneral) / 5.0;
    }
}