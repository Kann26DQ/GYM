using System.ComponentModel.DataAnnotations;

namespace GYM.Models
{
    public class Ejercicio
    {
        public int EjercicioId { get; set; }

        [Required]
        [StringLength(100)]
        public string Nombre { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string GrupoMuscular { get; set; } = string.Empty;

        [Required]
        [Range(1, 20)]
        public int Series { get; set; }

        [Required]
        [Range(1, 100)]
        public int Repeticiones { get; set; }

        [StringLength(50)]
        public string? Duracion { get; set; } // ✅ NUEVO: Para tiempo/descanso

        [StringLength(500)]
        public string? Notas { get; set; }

        // Relaciones
        public int RutinaId { get; set; }
        public Rutina? Rutina { get; set; }
    }
}
