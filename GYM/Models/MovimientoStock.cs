namespace GYM.Models
{
    public class MovimientoStock
    {
        public int MovimientoStockId { get; set; }
        public DateTime Fecha { get; set; }
        public string TipoMovimiento { get; set; } = string.Empty; // Entrada/Salida
        public int Cantidad { get; set; }

        // Relaciones
        public int ProductoId { get; set; }
        public Producto Producto { get; set; }

        public int EmpleadoId { get; set; }
        public Usuario Empleado { get; set; }
    }

}
