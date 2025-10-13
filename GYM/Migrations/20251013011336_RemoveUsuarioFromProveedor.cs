using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GYM.Migrations
{
    /// <inheritdoc />
    public partial class RemoveUsuarioFromProveedor : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Proveedores_Usuarios_UsuarioId",
                table: "Proveedores");

            migrationBuilder.DropIndex(
                name: "IX_Proveedores_UsuarioId",
                table: "Proveedores");

            migrationBuilder.DeleteData(
                table: "Roles",
                keyColumn: "RolId",
                keyValue: 4);

            migrationBuilder.DropColumn(
                name: "UsuarioId",
                table: "Proveedores");

            migrationBuilder.AddColumn<int>(
                name: "ProveedorId",
                table: "Usuarios",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Usuarios_ProveedorId",
                table: "Usuarios",
                column: "ProveedorId");

            migrationBuilder.AddForeignKey(
                name: "FK_Usuarios_Proveedores_ProveedorId",
                table: "Usuarios",
                column: "ProveedorId",
                principalTable: "Proveedores",
                principalColumn: "ProveedorId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Usuarios_Proveedores_ProveedorId",
                table: "Usuarios");

            migrationBuilder.DropIndex(
                name: "IX_Usuarios_ProveedorId",
                table: "Usuarios");

            migrationBuilder.DropColumn(
                name: "ProveedorId",
                table: "Usuarios");

            migrationBuilder.AddColumn<int>(
                name: "UsuarioId",
                table: "Proveedores",
                type: "int",
                nullable: true);

            migrationBuilder.InsertData(
                table: "Roles",
                columns: new[] { "RolId", "Nombre" },
                values: new object[] { 4, "Proveedor" });

            migrationBuilder.CreateIndex(
                name: "IX_Proveedores_UsuarioId",
                table: "Proveedores",
                column: "UsuarioId",
                unique: true,
                filter: "[UsuarioId] IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_Proveedores_Usuarios_UsuarioId",
                table: "Proveedores",
                column: "UsuarioId",
                principalTable: "Usuarios",
                principalColumn: "UsuarioId");
        }
    }
}
