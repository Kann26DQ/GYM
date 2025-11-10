namespace GYM.Models
{
    public class PlanAlimenticio
    {
        public int PlanAlimenticioId { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string Objetivo { get; set; } = string.Empty;

        // Relaciones - Cambiar a nullable
        public int? ClienteId { get; set; }  // 👈 Cambiar de int a int?
        public Usuario? Cliente { get; set; }

        public int? EmpleadoId { get; set; } // 👈 Cambiar de int a int?
        public Usuario? Empleado { get; set; }

        public ICollection<Comida> Comidas { get; set; }
    }

}
