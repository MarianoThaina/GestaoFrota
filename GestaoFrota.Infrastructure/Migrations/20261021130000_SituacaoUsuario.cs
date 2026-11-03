using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GestaoFrota.Infrastructure.Migrations
{
    /// <summary>
    /// US16: situação do usuário com três estados (Aguardando Aprovação, Ativo,
    /// Inativo) no lugar do booleano Ativo. Usuários existentes viram Ativo ou
    /// Inativo conforme o valor antigo; ninguém que já entrava fica bloqueado.
    /// Escrita à mão a partir do snapshot, como as anteriores.
    /// </summary>
    [DbContext(typeof(GestaoFrota.Infrastructure.Data.AppDbContext))]
    [Migration("20261021130000_SituacaoUsuario")]
    public partial class SituacaoUsuario : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Situacao",
                table: "Usuarios",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "Ativo");

            migrationBuilder.Sql("UPDATE [Usuarios] SET [Situacao] = 'Inativo' WHERE [Ativo] = 0");

            // Novas linhas passam a nascer AguardandoAprovacao (default do domínio).
            migrationBuilder.AlterColumn<string>(
                name: "Situacao",
                table: "Usuarios",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(30)",
                oldMaxLength: 30,
                oldDefaultValue: "Ativo");

            migrationBuilder.DropColumn(name: "Ativo", table: "Usuarios");

            migrationBuilder.CreateIndex(
                name: "IX_Usuarios_Situacao",
                table: "Usuarios",
                column: "Situacao");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(name: "IX_Usuarios_Situacao", table: "Usuarios");

            migrationBuilder.AddColumn<bool>(
                name: "Ativo",
                table: "Usuarios",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.Sql("UPDATE [Usuarios] SET [Ativo] = 0 WHERE [Situacao] <> 'Ativo'");

            migrationBuilder.DropColumn(name: "Situacao", table: "Usuarios");
        }
    }
}
