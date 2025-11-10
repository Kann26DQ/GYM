using GYM.Data;
using Microsoft.EntityFrameworkCore;

namespace GYM.Services
{
    public class MembresiaExpiradaBackgroundService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<MembresiaExpiradaBackgroundService> _logger;

        public MembresiaExpiradaBackgroundService(
            IServiceProvider serviceProvider,
            ILogger<MembresiaExpiradaBackgroundService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Servicio de verificación de membresías expiradas iniciado");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await VerificarMembresiasExpiradas();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error al verificar membresías expiradas");
                }

                // Ejecutar cada hora
                await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
            }
        }

        private async Task VerificarMembresiasExpiradas()
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDBContext>();

            var now = DateTime.UtcNow;

            // Obtener todas las membresías que están marcadas como activas pero ya expiraron
            var membresiasExpiradas = await context.MembresiasUsuarios
                .Include(m => m.Usuario)
                .Where(m => m.Activa && m.FechaFin < now)
                .ToListAsync();

            foreach (var membresia in membresiasExpiradas)
            {
                // Desactivar la membresía
                membresia.Activa = false;

                // Verificar si el usuario tiene otras membresías activas vigentes
                var tieneOtraMembresiaVigente = await context.MembresiasUsuarios
                    .AnyAsync(m => m.UsuarioId == membresia.UsuarioId &&
                                  m.MembresiaUsuarioId != membresia.MembresiaUsuarioId &&
                                  m.Activa &&
                                  m.FechaInicio <= now &&
                                  m.FechaFin >= now);

                // Si no tiene otra membresía vigente, desactivar la cuenta
                if (!tieneOtraMembresiaVigente && membresia.Usuario != null)
                {
                    membresia.Usuario.Activo = false;
                    _logger.LogInformation(
                        $"Cuenta desactivada: Usuario ID {membresia.UsuarioId} - {membresia.Usuario.Nombre} - Membresía expirada"
                    );
                }
            }

            if (membresiasExpiradas.Any())
            {
                await context.SaveChangesAsync();
                _logger.LogInformation($"Se procesaron {membresiasExpiradas.Count} membresías expiradas");
            }
        }
    }
}