namespace GYM.Models
{
    public class Usuario
    {
        public int UsuarioId { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string? Telefono { get; set; }
        public DateTime FechaRegistro { get; set; } = DateTime.Now;

        public bool Activo { get; set; } = true;

        // Relaciones
        public int RolId { get; set; }
        public Rol Rol { get; set; }

        public Proveedor? Proveedor { get; set; }

        public ICollection<Membresia> Membresias { get; set; }
        public ICollection<Rutina> Rutinas { get; set; }
        public ICollection<PlanAlimenticio> PlanesAlimenticios { get; set; }
        public ICollection<Venta> Ventas { get; set; }
        public ICollection<MovimientoStock> Movimientos { get; set; }
        public ICollection<Reporte> Reportes { get; set; }

        public ICollection<CartItem> CartItems { get; set; }
    }

}
