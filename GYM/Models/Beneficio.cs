namespace GYM.Models
{
    public class Beneficio
    {
        public int BeneficioId { get; set; }
        public string Descripcion { get; set; } = string.Empty;

        // Relaciones
        public int MembresiaId { get; set; }
        public Membresia Membresia { get; set; }
    }

}
