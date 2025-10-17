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

        // Relación con Usuario: nullable para evitar cambios forzados en la BD
        public int? UsuarioId { get; set; }
        public Usuario? Usuario { get; set; }
    }

}
