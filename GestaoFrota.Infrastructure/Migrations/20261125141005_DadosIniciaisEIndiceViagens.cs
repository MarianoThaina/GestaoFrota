using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace GestaoFrota.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class DadosIniciaisEIndiceViagens : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Viagens_VeiculoId",
                table: "Viagens");

            migrationBuilder.InsertData(
                table: "Categorias",
                columns: new[] { "Id", "Ativo", "DataAtualizacao", "DataCriacao", "Nome", "Tipo" },
                values: new object[,]
                {
                    { new Guid("00000000-0000-0000-0001-000000000001"), true, null, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Frete", "Receita" },
                    { new Guid("00000000-0000-0000-0001-000000000002"), true, null, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Combustível", "Despesa" },
                    { new Guid("00000000-0000-0000-0001-000000000003"), true, null, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Pedágio", "Despesa" },
                    { new Guid("00000000-0000-0000-0001-000000000004"), true, null, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Manutenção", "Despesa" },
                    { new Guid("00000000-0000-0000-0001-000000000005"), true, null, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Salários", "Despesa" },
                    { new Guid("00000000-0000-0000-0001-000000000006"), true, null, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Financiamento", "Despesa" }
                });

            migrationBuilder.InsertData(
                table: "FormasPagamento",
                columns: new[] { "Id", "Ativo", "DataAtualizacao", "DataCriacao", "Nome" },
                values: new object[,]
                {
                    { new Guid("00000000-0000-0000-0002-000000000001"), true, null, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Pix" },
                    { new Guid("00000000-0000-0000-0002-000000000002"), true, null, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Boleto" },
                    { new Guid("00000000-0000-0000-0002-000000000003"), true, null, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Transferência TED" },
                    { new Guid("00000000-0000-0000-0002-000000000004"), true, null, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Cartão Corporativo" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Categorias",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0001-000000000001"));

            migrationBuilder.DeleteData(
                table: "Categorias",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0001-000000000002"));

            migrationBuilder.DeleteData(
                table: "Categorias",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0001-000000000003"));

            migrationBuilder.DeleteData(
                table: "Categorias",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0001-000000000004"));

            migrationBuilder.DeleteData(
                table: "Categorias",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0001-000000000005"));

            migrationBuilder.DeleteData(
                table: "Categorias",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0001-000000000006"));

            migrationBuilder.DeleteData(
                table: "FormasPagamento",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0002-000000000001"));

            migrationBuilder.DeleteData(
                table: "FormasPagamento",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0002-000000000002"));

            migrationBuilder.DeleteData(
                table: "FormasPagamento",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0002-000000000003"));

            migrationBuilder.DeleteData(
                table: "FormasPagamento",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0002-000000000004"));

            migrationBuilder.CreateIndex(
                name: "IX_Viagens_VeiculoId",
                table: "Viagens",
                column: "VeiculoId");
        }
    }
}
