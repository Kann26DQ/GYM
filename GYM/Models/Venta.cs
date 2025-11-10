using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GYM.Models
{
    public class Venta
    {
        public int VentaId { get; set; }
        
        public DateTime Fecha { get; set; }
        
        [Column(TypeName = "decimal(18,2)")]
        public decimal Total { get; set; }
        
        // Relaciones - Cambiar ClienteId a nullable
        public int? ClienteId { get; set; }  // 👈 CAMBIO: De int a int?
        public Usuario? Cliente { get; set; }
        
        public int? EmpleadoId { get; set; } // Ya es nullable
        public Usuario? Empleado { get; set; }
        
        // Detalles
        public ICollection<DetalleVenta> Detalles { get; set; } = new List<DetalleVenta>();
    }
}
