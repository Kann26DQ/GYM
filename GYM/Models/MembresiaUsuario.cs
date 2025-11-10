using System.ComponentModel.DataAnnotations;

namespace GYM.Models
{
    public class MembresiaUsuario
    {
        public int MembresiaUsuarioId { get; set; }

        [Required]
        public int UsuarioId { get; set; }
        public Usuario? Usuario { get; set; }

        [Required]
        public int MembresiaPlanId { get; set; }
        public MembresiaPlan? Plan { get; set; }

        [Required]
        public DateTime FechaInicio { get; set; }

        [Required]
        public DateTime FechaFin { get; set; }

        [Range(0, 999999)]
        public decimal Precio { get; set; }

        public bool Activa { get; set; } = true;
    }
}