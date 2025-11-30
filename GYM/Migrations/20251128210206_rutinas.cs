using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GYM.Migrations
{
    /// <inheritdoc />
    public partial class rutinas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "GrupoClientesId",
                table: "EvaluacionesRendimiento",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "GruposClientes",
                columns: table => new
                {
                    GrupoClientesId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Titulo = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Color = table.Column<string>(type: "nvarchar(7)", maxLength: 7, nullable: false),
                    EmpleadoId = table.Column<int>(type: "int", nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Activo = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GruposClientes", x => x.GrupoClientesId);
                    table.ForeignKey(
                        name: "FK_GruposClientes_Usuarios_EmpleadoId",
                        column: x => x.EmpleadoId,
                        principalTable: "Usuarios",
                        principalColumn: "UsuarioId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EvaluacionesRendimiento_GrupoClientesId",
                table: "EvaluacionesRendimiento",
                column: "GrupoClientesId");

            migrationBuilder.CreateIndex(
                name: "IX_GruposClientes_EmpleadoId",
                table: "GruposClientes",
                column: "EmpleadoId");

            migrationBuilder.AddForeignKey(
                name: "FK_EvaluacionesRendimiento_GruposClientes_GrupoClientesId",
                table: "EvaluacionesRendimiento",
                column: "GrupoClientesId",
                principalTable: "GruposClientes",
                principalColumn: "GrupoClientesId",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_EvaluacionesRendimiento_GruposClientes_GrupoClientesId",
                table: "EvaluacionesRendimiento");

            migrationBuilder.DropTable(
                name: "GruposClientes");

            migrationBuilder.DropIndex(
                name: "IX_EvaluacionesRendimiento_GrupoClientesId",
                table: "EvaluacionesRendimiento");

            migrationBuilder.DropColumn(
                name: "GrupoClientesId",
                table: "EvaluacionesRendimiento");
        }
    }
}
