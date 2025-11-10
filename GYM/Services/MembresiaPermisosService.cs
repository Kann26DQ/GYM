using GYM.Data;
using Microsoft.EntityFrameworkCore;

namespace GYM.Services
{
    public class MembresiaPermisosService
    {
        private readonly AppDBContext _context;

        public MembresiaPermisosService(AppDBContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Verifica si el usuario tiene permiso para ver rutinas
        /// </summary>
        public async Task<bool> TienePermisoRutina(int usuarioId)
        {
            var now = DateTime.UtcNow;
            var membresia = await _context.MembresiasUsuarios
                .Include(m => m.Plan)
                .Where(m => m.UsuarioId == usuarioId &&
                           m.Activa &&
                           m.FechaInicio <= now &&
                           m.FechaFin >= now)
                .OrderByDescending(m => m.FechaInicio)
                .FirstOrDefaultAsync();

            return membresia?.Plan?.PermiteRutina ?? false;
        }

        /// <summary>
        /// Verifica si el usuario tiene permiso para ver planes alimenticios
        /// </summary>
        public async Task<bool> TienePermisoAlimentacion(int usuarioId)
        {
            var now = DateTime.UtcNow;
            var membresia = await _context.MembresiasUsuarios
                .Include(m => m.Plan)
                .Where(m => m.UsuarioId == usuarioId &&
                           m.Activa &&
                           m.FechaInicio <= now &&
                           m.FechaFin >= now)
                .OrderByDescending(m => m.FechaInicio)
                .FirstOrDefaultAsync();

            return membresia?.Plan?.PermiteAlimentacion ?? false;
        }

        /// <summary>
        /// Obtiene los permisos de la membresía activa del usuario
        /// </summary>
        public async Task<(bool tieneRutina, bool tieneAlimentacion)> ObtenerPermisos(int usuarioId)
        {
            var now = DateTime.UtcNow;
            var membresia = await _context.MembresiasUsuarios
                .Include(m => m.Plan)
                .Where(m => m.UsuarioId == usuarioId &&
                           m.Activa &&
                           m.FechaInicio <= now &&
                           m.FechaFin >= now)
                .OrderByDescending(m => m.FechaInicio)
                .FirstOrDefaultAsync();

            if (membresia?.Plan == null)
                return (false, false);

            return (membresia.Plan.PermiteRutina, membresia.Plan.PermiteAlimentacion);
        }
    }
}