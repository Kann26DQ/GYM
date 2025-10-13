namespace GYM.Models
{
    public class Comida
    {
        public int ComidaId { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string Ingredientes { get; set; } = string.Empty;
        public string Horarios { get; set; } = string.Empty;

        // Relaciones
        public int PlanAlimenticioId { get; set; }
        public PlanAlimenticio PlanAlimenticio { get; set; }
    }

}
