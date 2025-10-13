namespace GYM.Models
{
    public class Rol
    {
        public int RolId { get; set; }
        public string Nombre { get; set; } = string.Empty;

        // Relaciones
        public ICollection<Usuario> Usuarios { get; set; }
    }
}
