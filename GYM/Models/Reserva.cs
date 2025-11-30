using System;
using System.ComponentModel.DataAnnotations;

namespace GYM.Models
{
    public class Reserva
    {
        public int ReservaId { get; set; }

        [Required]
        public int UsuarioId { get; set; }
        public Usuario? Usuario { get; set; }

        [Required]
        [Display(Name = "Fecha de Reserva")]
        public DateTime FechaReserva { get; set; }

        [Required]
        [Display(Name = "Hora de Inicio")]
        public TimeSpan HoraInicio { get; set; }

        [Required]
        [Display(Name = "Hora de Fin")]
        public TimeSpan HoraFin { get; set; }

        [Display(Name = "Estado")]
        public EstadoReserva Estado { get; set; } = EstadoReserva.Pendiente;

        [Display(Name = "Fecha de Creación")]
        public DateTime FechaCreacion { get; set; } = DateTime.Now;

        [Display(Name = "Tipo de Entrenamiento")]
        [StringLength(50)]
        public string? TipoEntrenamiento { get; set; }

        [Display(Name = "Notas")]
        [StringLength(500)]
        public string? Notas { get; set; }

        // ✅ CAMPOS PARA ASISTENCIA
        [Display(Name = "Asistió")]
        public bool? Asistio { get; set; }

        [Display(Name = "Fecha de Marcado")]
        public DateTime? FechaMarcado { get; set; }

        [Display(Name = "Marcado Por")]
        public int? MarcadoPorId { get; set; }
        public Usuario? MarcadoPor { get; set; }

        [Display(Name = "Observaciones de Asistencia")]
        [StringLength(250)]
        public string? ObservacionesAsistencia { get; set; }

        [Display(Name = "Duración (horas)")]
        [Range(1, 4)]
        public int DuracionHoras { get; set; } = 2;
    }

    public enum EstadoReserva
    {
        Pendiente = 0,
        Confirmada = 1,
        Completada = 2,
        Cancelada = 3
    }
}