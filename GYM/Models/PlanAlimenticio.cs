namespace GYM.Models
{
    public class PlanAlimenticio
    {
        public int PlanAlimenticioId { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string Objetivo { get; set; } = string.Empty;

        // Relaciones
        public int ClienteId { get; set; }
        public Usuario Cliente { get; set; }

        public int EmpleadoId { get; set; }
        public Usuario Empleado { get; set; }

        public ICollection<Comida> Comidas { get; set; }
    }

}
