using System;
using System.ComponentModel.DataAnnotations;

namespace GYM.Models
{
    public class Reserva
    {
        public int ReservaId { get; set; }

        [Required]
        public int UsuarioId { get; set; }

        // 👇 CORRECCIÓN: Hacer nullable para evitar error de validación
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

        [Display(Name = "Notas")]
        [StringLength(500)]
        public string? Notas { get; set; }
    }

    public enum EstadoReserva
    {
        Pendiente = 0,
        Confirmada = 1,
        Completada = 2,
        Cancelada = 3
    }
}
