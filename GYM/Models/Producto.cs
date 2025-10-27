using System.ComponentModel.DataAnnotations;

namespace GYM.Models
{
    public class Producto
    {
        public int ProductoId { get; set; }

        [Required(ErrorMessage = "El nombre es obligatorio")]
        public string Nombre { get; set; } = string.Empty;

        public string Descripcion { get; set; } = string.Empty;

        [Range(0, double.MaxValue, ErrorMessage = "El precio no puede ser negativo.")]
        public decimal Precio { get; set; }

        [Range(0, int.MaxValue, ErrorMessage = "El stock no puede ser negativo.")]
        public int Stock { get; set; }

        // Cupo disponible para la venta (no sobrepasa el Stock)
        [Range(0, int.MaxValue)]
        public int StockVenta { get; set; } = 0;

        public int? ProveedorId { get; set; }
        public Proveedor? Proveedor { get; set; }

        public DateTime FechaRegistro { get; set; } = DateTime.Now;

        // Derivado: en venta si hay cupo y stock
        public bool Disponible { get; set; } = false;

        public ICollection<MovimientoStock> Movimientos { get; set; } = new List<MovimientoStock>();
        public ICollection<DetalleVenta> Detalles { get; set; } = new List<DetalleVenta>();
        public ICollection<CartItem> CartItems { get; set; } = new List<CartItem>();
    }
}