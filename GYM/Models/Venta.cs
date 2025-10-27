namespace GYM.Models
{
    public class Venta
    {
        public int VentaId { get; set; }
        public DateTime Fecha { get; set; }
        public decimal Total { get; set; }

        // Relaciones
        public int ClienteId { get; set; }
        public Usuario Cliente { get; set; }

        public int? EmpleadoId { get; set; } // Permitir null para clientes
        public Usuario Empleado { get; set; }

        public ICollection<DetalleVenta> Detalles { get; set; }
    }
}
