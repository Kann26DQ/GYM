namespace GYM.Models
{
   public class Proveedor
{
        public int ProveedorId { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string? Telefono { get; set; }
        public string? Email { get; set; }
        public string? Direccion { get; set; }
        public bool Estado { get; set; } = true;



    public ICollection<Producto> Productos { get; set; }
}

}
