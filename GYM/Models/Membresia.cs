namespace GYM.Models
{
    public class Membresia
    {
        public int MembresiaId { get; set; }
        public string Tipo { get; set; } = string.Empty;
        public decimal Precio { get; set; }
        public DateTime FechaInicio { get; set; }
        public DateTime FechaFin { get; set; }

        // Relaciones
        public int ClienteId { get; set; }
        public Usuario Cliente { get; set; }

        public int EmpleadoId { get; set; }
        public Usuario Empleado { get; set; }

        public ICollection<Beneficio> Beneficios { get; set; }
    }

}
