namespace GYM.Models
{
    public class Reporte
    {
        public int ReporteId { get; set; }
        public DateTime FechaGeneracion { get; set; }
        public DateTime FechaInicio { get; set; }
        public DateTime FechaFin { get; set; }
        public decimal TotalVentas { get; set; }

        // Relaciones
        public int EmpleadoId { get; set; }
        public Usuario Empleado { get; set; }
    }

}
