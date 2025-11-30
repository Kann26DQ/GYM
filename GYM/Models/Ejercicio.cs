using System.ComponentModel.DataAnnotations;

namespace GYM.Models
{
    public class Ejercicio
    {
        public int EjercicioId { get; set; }

        [Required(ErrorMessage = "El nombre del ejercicio es obligatorio")]
        [StringLength(100, ErrorMessage = "El nombre no puede exceder 100 caracteres")]
        [RegularExpression(@"^[a-zA-ZáéíóúÁÉÍÓÚñÑ]+$", ErrorMessage = "El nombre solo puede contener letras sin espacios ni símbolos")]
        public string Nombre { get; set; } = string.Empty;

        [Required(ErrorMessage = "El grupo muscular es obligatorio")]
        [StringLength(50)]
        public string GrupoMuscular { get; set; } = string.Empty;

        [Required(ErrorMessage = "Las series son obligatorias")]
        [Range(1, 20, ErrorMessage = "Las series deben estar entre 1 y 20")]
        public int Series { get; set; }

        [Required(ErrorMessage = "Las repeticiones son obligatorias")]
        [Range(1, 100, ErrorMessage = "Las repeticiones deben estar entre 1 y 100")]
        public int Repeticiones { get; set; }

        [StringLength(50, ErrorMessage = "La duración no puede exceder 50 caracteres")]
        public string? Duracion { get; set; }

        [StringLength(500, ErrorMessage = "Las notas no pueden exceder 500 caracteres")]
        public string? Notas { get; set; }

        // Relaciones
        public int RutinaId { get; set; }
        public Rutina? Rutina { get; set; }
    }
}