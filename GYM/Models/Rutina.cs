namespace GYM.Models
{
    public class Rutina
    {
        public int RutinaId { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string Tipo { get; set; } = string.Empty;
        public int DuracionSemanas { get; set; }

        // Relaciones - Cambiar a nullable
        public int? ClienteId { get; set; }  // 👈 Cambiar de int a int?
        public Usuario? Cliente { get; set; }

        public int? EmpleadoId { get; set; } // 👈 Cambiar de int a int?
        public Usuario? Empleado { get; set; }

        public ICollection<Ejercicio> Ejercicios { get; set; }
    }

}
