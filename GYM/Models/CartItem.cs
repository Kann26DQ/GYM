namespace GYM.Models
{
    public class CartItem
    {
        public int CartItemId { get; set; } // Clave primaria

        public int ProductoId { get; set; } // Relación con Producto
        public Producto Producto { get; set; } // Navegación al Producto

        public int Cantidad { get; set; } // Cantidad del producto en el carrito

        public int? UsuarioId { get; set; } // Relación opcional con Usuario (cliente)
        public Usuario Usuario { get; set; } // Navegación al Usuario
    }
}