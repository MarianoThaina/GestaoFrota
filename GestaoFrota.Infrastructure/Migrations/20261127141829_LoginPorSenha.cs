using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GestaoFrota.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class LoginPorSenha : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Usuarios_GoogleId",
                table: "Usuarios");

            migrationBuilder.AlterColumn<string>(
                name: "GoogleId",
                table: "Usuarios",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100);

            migrationBuilder.AddColumn<DateTime>(
                name: "BloqueadoAte",
                table: "Usuarios",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SenhaHash",
                table: "Usuarios",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TentativasLoginFalhas",
                table: "Usuarios",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Usuarios_GoogleId",
                table: "Usuarios",
                column: "GoogleId",
                unique: true,
                filter: "[GoogleId] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Usuarios_GoogleId",
                table: "Usuarios");

            migrationBuilder.DropColumn(
                name: "BloqueadoAte",
                table: "Usuarios");

            migrationBuilder.DropColumn(
                name: "SenhaHash",
                table: "Usuarios");

            migrationBuilder.DropColumn(
                name: "TentativasLoginFalhas",
                table: "Usuarios");

            migrationBuilder.AlterColumn<string>(
                name: "GoogleId",
                table: "Usuarios",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Usuarios_GoogleId",
                table: "Usuarios",
                column: "GoogleId",
                unique: true);
        }
    }
}
