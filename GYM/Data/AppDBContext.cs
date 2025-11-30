using GYM.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace GYM.Data
{
    public class AppDBContext : DbContext
    {
        public AppDBContext(DbContextOptions<AppDBContext> options) : base(options) { }

        public DbSet<Rol> Roles { get; set; }
        public DbSet<Usuario> Usuarios { get; set; }
        public DbSet<MembresiaPlan> MembresiaPlanes { get; set; }
        public DbSet<MembresiaUsuario> MembresiasUsuarios { get; set; }
        public DbSet<Reserva> Reservas { get; set; }
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
        public DbSet<CartItem> CartItems { get; set; }
        public DbSet<HorarioFijo> HorariosFijos { get; set; }
        public DbSet<EvaluacionRendimiento> EvaluacionesRendimiento { get; set; }
        public DbSet<GrupoClientes> GruposClientes { get; set; } // ✅ AGREGADO

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Roles seed
            modelBuilder.Entity<Rol>().HasData(
                new Rol { RolId = 1, Nombre = "Cliente" },
                new Rol { RolId = 2, Nombre = "Gymbro" },
                new Rol { RolId = 3, Nombre = "SuperAdmin" }
            );

            // MembresiaPlan -> tabla "Membresias"
            modelBuilder.Entity<MembresiaPlan>().ToTable("Membresias");

            // Seed de planes (2 Mensuales + 2 Anuales)
            modelBuilder.Entity<MembresiaPlan>().HasData(
                // PLANES MENSUALES
                new MembresiaPlan
                {
                    MembresiaPlanId = 1,
                    Nombre = "Membresía Básica",
                    Descripcion = "Acceso a rutinas de entrenamiento personalizadas diseñadas por nuestros profesionales.",
                    Precio = 50m,
                    PermiteRutina = true,
                    PermiteAlimentacion = false,
                    Activo = true,
                    DuracionDias = 30,
                    BeneficiosTexto = "Acceso a rutinas personalizadas; Actualización semanal de ejercicios; Seguimiento de progreso"
                },
                new MembresiaPlan
                {
                    MembresiaPlanId = 2,
                    Nombre = "Membresía Completa",
                    Descripcion = "Acceso total a rutinas de entrenamiento y planes alimenticios personalizados para alcanzar tus objetivos.",
                    Precio = 120m,
                    PermiteRutina = true,
                    PermiteAlimentacion = true,
                    Activo = true,
                    DuracionDias = 30,
                    BeneficiosTexto = "Acceso a rutinas personalizadas; Plan alimenticio adaptado a tus metas; Actualización semanal de ejercicios y comidas; Seguimiento completo de progreso; Asesoría nutricional básica"
                },
                // PLANES ANUALES
                new MembresiaPlan
                {
                    MembresiaPlanId = 3,
                    Nombre = "Membresía Básica Anual",
                    Descripcion = "Ahorra con nuestro plan anual. Acceso a rutinas de entrenamiento durante todo el año.",
                    Precio = 500m,
                    PermiteRutina = true,
                    PermiteAlimentacion = false,
                    Activo = true,
                    DuracionDias = 365,
                    BeneficiosTexto = "Acceso ilimitado a rutinas personalizadas; Actualización semanal de ejercicios; Seguimiento de progreso; Descuento especial anual; Sin renovaciones mensuales"
                },
                new MembresiaPlan
                {
                    MembresiaPlanId = 4,
                    Nombre = "Membresía Completa Anual",
                    Descripcion = "El mejor valor del año. Acceso completo a rutinas y planes alimenticios con ahorro significativo.",
                    Precio = 1200m,
                    PermiteRutina = true,
                    PermiteAlimentacion = true,
                    Activo = true,
                    DuracionDias = 365,
                    BeneficiosTexto = "Acceso a rutinas personalizadas; Plan alimenticio adaptado a tus metas; Actualización semanal de ejercicios y comidas; Seguimiento completo de progreso; Asesoría nutricional completa; Descuento anual del 16%; Prioridad en reservas"
                }
            );

            // Precisión decimales
            modelBuilder.Entity<Producto>().Property(p => p.Precio).HasPrecision(18, 2);
            modelBuilder.Entity<DetalleVenta>().Property(dv => dv.PrecioUnitario).HasPrecision(18, 2);
            modelBuilder.Entity<Venta>().Property(v => v.Total).HasPrecision(18, 2);
            modelBuilder.Entity<Reporte>().Property(r => r.TotalVentas).HasPrecision(18, 2);
            modelBuilder.Entity<MembresiaPlan>().Property(mp => mp.Precio).HasPrecision(18, 2);
            modelBuilder.Entity<MembresiaUsuario>().Property(mu => mu.Precio).HasPrecision(18, 2);

            // ✅ Precisión para EvaluacionRendimiento
            modelBuilder.Entity<EvaluacionRendimiento>()
                .Property(e => e.Peso)
                .HasPrecision(18, 2);

            modelBuilder.Entity<EvaluacionRendimiento>()
                .Property(e => e.Altura)
                .HasPrecision(18, 2);

            modelBuilder.Entity<EvaluacionRendimiento>()
                .Property(e => e.IMC)
                .HasPrecision(18, 2);

            // ✅ Relaciones de EvaluacionRendimiento
            modelBuilder.Entity<EvaluacionRendimiento>()
                .HasOne(e => e.Cliente)
                .WithMany()
                .HasForeignKey(e => e.ClienteId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<EvaluacionRendimiento>()
                .HasOne(e => e.Empleado)
                .WithMany()
                .HasForeignKey(e => e.EmpleadoId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<EvaluacionRendimiento>()
                .HasOne(e => e.Grupo)
                .WithMany(g => g.Evaluaciones)
                .HasForeignKey(e => e.GrupoClientesId)
                .OnDelete(DeleteBehavior.SetNull);

            // ✅ Relación Rutina -> EvaluacionRendimiento
            modelBuilder.Entity<Rutina>()
                .HasOne(r => r.EvaluacionBase)
                .WithMany(e => e.Rutinas)
                .HasForeignKey(r => r.EvaluacionRendimientoId)
                .OnDelete(DeleteBehavior.SetNull);

            // ✅ Relación GrupoClientes -> Empleado
            modelBuilder.Entity<GrupoClientes>()
                .HasOne(g => g.Empleado)
                .WithMany()
                .HasForeignKey(g => g.EmpleadoId)
                .OnDelete(DeleteBehavior.Restrict);

            // Relación de asignaciones de membresías
            modelBuilder.Entity<MembresiaUsuario>()
                .HasOne(mu => mu.Usuario)
                .WithMany()
                .HasForeignKey(mu => mu.UsuarioId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<MembresiaUsuario>()
                .HasOne(mu => mu.Plan)
                .WithMany()
                .HasForeignKey(mu => mu.MembresiaPlanId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<MembresiaUsuario>()
                .HasIndex(mu => new { mu.UsuarioId, mu.FechaFin });

            // RUTINA
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

            // PLAN ALIMENTICIO
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

            // VENTA
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

            // MOVIMIENTO STOCK
            modelBuilder.Entity<MovimientoStock>()
                .HasOne(ms => ms.Usuario)
                .WithMany(u => u.Movimientos)
                .HasForeignKey(ms => ms.UsuarioId)
                .OnDelete(DeleteBehavior.Restrict);

            // REPORTE
            modelBuilder.Entity<Reporte>()
                .HasOne(r => r.Empleado)
                .WithMany(u => u.Reportes)
                .HasForeignKey(r => r.EmpleadoId)
                .OnDelete(DeleteBehavior.Restrict);

            // PROVEEDOR
            modelBuilder.Entity<Proveedor>()
                .HasMany(p => p.Productos)
                .WithOne(p => p.Proveedor)
                .HasForeignKey(p => p.ProveedorId)
                .OnDelete(DeleteBehavior.SetNull);

            // CartItem
            modelBuilder.Entity<CartItem>()
                .HasOne(ci => ci.Producto)
                .WithMany(p => p.CartItems)
                .HasForeignKey(ci => ci.ProductoId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<CartItem>()
                .HasOne(ci => ci.Usuario)
                .WithMany(u => u.CartItems)
                .HasForeignKey(ci => ci.UsuarioId)
                .OnDelete(DeleteBehavior.Restrict);

            // Configuración de Reserva
            modelBuilder.Entity<Reserva>()
                .HasOne(r => r.Usuario)
                .WithMany()
                .HasForeignKey(r => r.UsuarioId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Reserva>()
                .HasIndex(r => new { r.FechaReserva, r.HoraInicio, r.HoraFin });

            modelBuilder.Entity<Reserva>(entity =>
            {
                entity.HasKey(r => r.ReservaId);

                entity.HasOne(r => r.Usuario)
                    .WithMany()
                    .HasForeignKey(r => r.UsuarioId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(r => r.MarcadoPor)
                    .WithMany()
                    .HasForeignKey(r => r.MarcadoPorId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(r => new { r.FechaReserva, r.HoraInicio, r.HoraFin });

                entity.Property(r => r.Estado)
                    .HasConversion<int>();

                entity.Property(r => r.Asistio)
                    .IsRequired(false);
            });
        }
    }
}