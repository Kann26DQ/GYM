namespace GYM.Models
{
    public class Ejercicio
    {
        public int EjercicioId { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string GrupoMuscular { get; set; } = string.Empty;
        public int Series { get; set; }
        public int Repeticiones { get; set; }
        public string? Notas { get; set; }

        // Relaciones
        public int RutinaId { get; set; }
        public Rutina Rutina { get; set; }
    }

}
