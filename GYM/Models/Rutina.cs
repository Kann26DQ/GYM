using System.ComponentModel.DataAnnotations;

namespace GYM.Models
{
    public class Rutina
    {
        public int RutinaId { get; set; }

        [Required]
        [Display(Name = "Nombre de la Rutina")]
        [StringLength(100)]
        public string Nombre { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Tipo de Rutina")]
        [StringLength(50)]
        public string Tipo { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Duración (semanas)")]
        [Range(1, 52)]
        public int DuracionSemanas { get; set; }

        [Display(Name = "Descripción")]
        [StringLength(500)]
        public string? Descripcion { get; set; }

        [Display(Name = "Nivel de Dificultad")]
        [StringLength(20)]
        public string? NivelDificultad { get; set; }

        [Display(Name = "Día de la Semana")]
        public DayOfWeek? DiaSemana { get; set; }

        [Display(Name = "Hora de Inicio")]
        public TimeSpan? HoraInicio { get; set; }

        [Display(Name = "Hora de Fin")]
        public TimeSpan? HoraFin { get; set; }

        [Display(Name = "Fecha Específica")]
        public DateTime? FechaEspecifica { get; set; }

        [Required]
        [Display(Name = "Fecha de Creación")]
        public DateTime FechaCreacion { get; set; } = DateTime.Now;

        [Display(Name = "Fecha de Inicio")]
        public DateTime? FechaInicio { get; set; }

        [Display(Name = "Fecha de Fin")]
        public DateTime? FechaFin { get; set; }

        [Display(Name = "Activa")]
        public bool Activa { get; set; } = true;

        // Relaciones
        [Required]
        public int ClienteId { get; set; }
        public Usuario? Cliente { get; set; }

        [Required]
        public int EmpleadoId { get; set; }
        public Usuario? Empleado { get; set; }

        public int? EvaluacionRendimientoId { get; set; }
        public EvaluacionRendimiento? EvaluacionBase { get; set; }

        // ✅ CRÍTICO: Inicializar la colección para evitar duplicados
        public ICollection<Ejercicio> Ejercicios { get; set; } = new List<Ejercicio>();
    }
}