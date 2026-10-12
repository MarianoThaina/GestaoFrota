using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GestaoFrota.Infrastructure.Migrations
{
    /// <summary>
    /// Núcleo financeiro (US03/US04/US05): status e datas em Movimentacoes,
    /// comprovante em banco e novos campos de Dividas.
    /// Escrita à mão a partir do snapshot; se preferir, apague-a e gere a sua
    /// com: dotnet ef migrations add FinanceiroCore -p GestaoFrota.Infrastructure -s GestaoFrota.API
    /// </summary>
    [DbContext(typeof(GestaoFrota.Infrastructure.Data.AppDbContext))]
    [Migration("20261011120000_FinanceiroCore")]
    public partial class FinanceiroCore : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ---------- Dividas ----------
            migrationBuilder.AddColumn<DateTime>(
                name: "DataPrimeiroVencimento",
                table: "Dividas",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<decimal>(
                name: "ValorParcela",
                table: "Dividas",
                type: "decimal(14,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "ValorQuitacaoAntecipada",
                table: "Dividas",
                type: "decimal(14,2)",
                nullable: true);

            // ---------- Movimentacoes ----------
            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "Movimentacoes",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Pago");

            migrationBuilder.AddColumn<DateTime>(
                name: "DataVencimento",
                table: "Movimentacoes",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "DataPagamento",
                table: "Movimentacoes",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "ComprovanteConteudo",
                table: "Movimentacoes",
                type: "varbinary(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ComprovanteTipo",
                table: "Movimentacoes",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            // Linhas pré-existentes (se houver) eram lançamentos já realizados:
            // ficam como Pago, com vencimento/pagamento = data do lançamento.
            migrationBuilder.Sql(
                "UPDATE [Movimentacoes] SET [DataVencimento] = [Data], [DataPagamento] = [Data] WHERE [DataPagamento] IS NULL");

            // Novas linhas passam a nascer Pendentes (default do domínio).
            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "Movimentacoes",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(20)",
                oldMaxLength: 20,
                oldDefaultValue: "Pago");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Movimentacao_PagoExigeDataPagamento",
                table: "Movimentacoes",
                sql: "[Status] <> 'Pago' OR [DataPagamento] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Movimentacoes_Status_DataVencimento",
                table: "Movimentacoes",
                columns: new[] { "Status", "DataVencimento" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Movimentacoes_Status_DataVencimento",
                table: "Movimentacoes");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Movimentacao_PagoExigeDataPagamento",
                table: "Movimentacoes");

            migrationBuilder.DropColumn(name: "ComprovanteTipo", table: "Movimentacoes");
            migrationBuilder.DropColumn(name: "ComprovanteConteudo", table: "Movimentacoes");
            migrationBuilder.DropColumn(name: "DataPagamento", table: "Movimentacoes");
            migrationBuilder.DropColumn(name: "DataVencimento", table: "Movimentacoes");
            migrationBuilder.DropColumn(name: "Status", table: "Movimentacoes");

            migrationBuilder.DropColumn(name: "ValorQuitacaoAntecipada", table: "Dividas");
            migrationBuilder.DropColumn(name: "ValorParcela", table: "Dividas");
            migrationBuilder.DropColumn(name: "DataPrimeiroVencimento", table: "Dividas");
        }
    }
}
