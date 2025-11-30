using System;
using System.ComponentModel.DataAnnotations;

namespace GYM.Models
{
    public class HorarioFijo
    {
        public int HorarioFijoId { get; set; }

        [Required]
        public int UsuarioId { get; set; }
        public Usuario? Usuario { get; set; }

        [Required]
        [Display(Name = "Día de la Semana")]
        public DayOfWeek DiaSemana { get; set; }

        [Required]
        [Display(Name = "Hora de Inicio")]
        public TimeSpan HoraInicio { get; set; }

        [Required]
        [Display(Name = "Hora de Fin")]
        public TimeSpan HoraFin { get; set; }

        [Required]
        [Display(Name = "Tipo de Entrenamiento")]
        [StringLength(50)]
        public string TipoEntrenamiento { get; set; } = string.Empty;

        [Display(Name = "Notas")]
        [StringLength(500)]
        public string? Notas { get; set; }

        [Display(Name = "Activo")]
        public bool Activo { get; set; } = true;

        [Display(Name = "Fecha de Creación")]
        public DateTime FechaCreacion { get; set; } = DateTime.Now;
    }
}