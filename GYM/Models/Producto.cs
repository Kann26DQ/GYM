namespace GYM.Models
{
    public class Producto
    {
        public int ProductoId { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public decimal Precio { get; set; }
        public int Stock { get; set; }

        public int? ProveedorId { get; set; }
        public Proveedor? Proveedor { get; set; }

        // Relaciones
        public ICollection<MovimientoStock> Movimientos { get; set; }
        public ICollection<DetalleVenta> Detalles { get; set; }
    }

}
