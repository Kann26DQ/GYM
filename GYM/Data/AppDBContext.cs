using GYM.Models;
using Microsoft.EntityFrameworkCore;

namespace GYM.Data
{
    public class AppDBContext : DbContext
    {
        public AppDBContext(DbContextOptions<AppDBContext> options) : base(options) { }

        public DbSet<Rol> Roles { get; set; }
        public DbSet<Usuario> Usuarios { get; set; }
        public DbSet<Membresia> Membresias { get; set; }
        public DbSet<Beneficio> Beneficios { get; set; }
        public DbSet<Rutina> Rutinas { get; set; }
        public DbSet<Ejercicio> Ejercicios { get; set; }
        public DbSet<PlanAlimenticio> PlanesAlimenticios { get; set; }
        public DbSet<Comida> Comidas { get; set; }
        public DbSet<Producto> Productos { get; set; }
        public DbSet<MovimientoStock> MovimientosStock { get; set; }
        public DbSet<Venta> Ventas { get; set; }
        public DbSet<DetalleVenta> DetallesVenta { get; set; }
        public DbSet<Reporte> Reportes { get; set; }
        public DbSet<Proveedor> Proveedores { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Rol>().HasData(
            new Rol { RolId = 1, Nombre = "Cliente" },
            new Rol { RolId = 2, Nombre = "Gymbro" },
            new Rol { RolId = 3, Nombre = "SuperAdmin" }
             );
            // -------------------
            // MEMBRESIA
            // -------------------
            modelBuilder.Entity<Membresia>()
                .HasOne(m => m.Cliente)
                .WithMany(u => u.Membresias)
                .HasForeignKey(m => m.ClienteId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Membresia>()
                .HasOne(m => m.Empleado)
                .WithMany()
                .HasForeignKey(m => m.EmpleadoId)
                .OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Membresia>()
                .Property(m => m.Precio)
                .HasPrecision(10, 2);
            modelBuilder.Entity<Producto>()
                .Property(p => p.Precio)
                .HasPrecision(10, 2);
            modelBuilder.Entity<DetalleVenta>()
                .Property(dv => dv.PrecioUnitario)
                .HasPrecision(10, 2);

            // -------------------
            // RUTINA
            // -------------------
            modelBuilder.Entity<Rutina>()
                .HasOne(r => r.Cliente)
                .WithMany(u => u.Rutinas)
                .HasForeignKey(r => r.ClienteId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Rutina>()
                .HasOne(r => r.Empleado)
                .WithMany()
                .HasForeignKey(r => r.EmpleadoId)
                .OnDelete(DeleteBehavior.Restrict);

            // -------------------
            // PLAN ALIMENTICIO
            // -------------------
            modelBuilder.Entity<PlanAlimenticio>()
                .HasOne(p => p.Cliente)
                .WithMany(u => u.PlanesAlimenticios)
                .HasForeignKey(p => p.ClienteId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<PlanAlimenticio>()
                .HasOne(p => p.Empleado)
                .WithMany()
                .HasForeignKey(p => p.EmpleadoId)
                .OnDelete(DeleteBehavior.Restrict);

            // -------------------
            // VENTA
            // -------------------
            modelBuilder.Entity<Venta>()
                .HasOne(v => v.Cliente)
                .WithMany(u => u.Ventas)
                .HasForeignKey(v => v.ClienteId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Venta>()
                .HasOne(v => v.Empleado)
                .WithMany()
                .HasForeignKey(v => v.EmpleadoId)
                .OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Venta>()
    .Property(v => v.Total)
    .HasPrecision(10, 2);

            // -------------------
            // MOVIMIENTO STOCK
            // -------------------
            modelBuilder.Entity<MovimientoStock>()
                .HasOne(ms => ms.Empleado)
                .WithMany(u => u.Movimientos)
                .HasForeignKey(ms => ms.EmpleadoId)
                .OnDelete(DeleteBehavior.Restrict);

            // -------------------
            // REPORTE
            // -------------------
            modelBuilder.Entity<Reporte>()
                .HasOne(r => r.Empleado)
                .WithMany(u => u.Reportes)
                .HasForeignKey(r => r.EmpleadoId)
                .OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Reporte>()
    .Property(r => r.TotalVentas)
    .HasPrecision(10, 2);

            // -------------------
            // PROVEEDOR
            // -------------------
            modelBuilder.Entity<Proveedor>()
                .HasMany(p => p.Productos)
                .WithOne(p => p.Proveedor)
                .HasForeignKey(p => p.ProveedorId)
                .OnDelete(DeleteBehavior.SetNull);
        }
    }
}
