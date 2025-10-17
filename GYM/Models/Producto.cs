namespace GYM.Models
{
    public class Producto
    {
        public int ProductoId { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string Descripcion { get; set; }
        public decimal Precio { get; set; }
        public int Stock { get; set; }

        public int? ProveedorId { get; set; }
        public Proveedor? Proveedor { get; set; }

        public DateTime FechaRegistro { get; set; } = DateTime.Now;
        // Nueva propiedad: si el producto está disponible para venta
        public bool Disponible { get; set; } = false;

        // Relaciones
        public ICollection<MovimientoStock> Movimientos { get; set; }
        public ICollection<DetalleVenta> Detalles { get; set; }
    }

}
