using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace GYM.Migrations
{
    /// <inheritdoc />
    public partial class plan : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Membresias",
                columns: new[] { "MembresiaPlanId", "Activo", "BeneficiosTexto", "Descripcion", "DuracionDias", "Nombre", "PermiteAlimentacion", "PermiteRutina", "Precio", "UsuarioId" },
                values: new object[,]
                {
                    { 3, true, "Acceso ilimitado a rutinas personalizadas; Actualización semanal de ejercicios; Seguimiento de progreso; Descuento especial anual; Sin renovaciones mensuales", "Ahorra con nuestro plan anual. Acceso a rutinas de entrenamiento durante todo el año.", 365, "Membresía Básica Anual", false, true, 500m, null },
                    { 4, true, "Acceso a rutinas personalizadas; Plan alimenticio adaptado a tus metas; Actualización semanal de ejercicios y comidas; Seguimiento completo de progreso; Asesoría nutricional completa; Descuento anual del 16%; Prioridad en reservas", "El mejor valor del año. Acceso completo a rutinas y planes alimenticios con ahorro significativo.", 365, "Membresía Completa Anual", true, true, 1200m, null }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Membresias",
                keyColumn: "MembresiaPlanId",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "Membresias",
                keyColumn: "MembresiaPlanId",
                keyValue: 4);
        }
    }
}
