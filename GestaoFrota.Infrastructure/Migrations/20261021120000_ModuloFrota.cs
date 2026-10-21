using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GestaoFrota.Infrastructure.Migrations
{
    /// <summary>
    /// Módulo de frota (US06, US06-b, US08, US09): capacidade de carga no
    /// veículo, quilometragem rodada persistida na viagem, litros na
    /// movimentação e a tabela de manutenções.
    /// Escrita à mão a partir do snapshot, como a FinanceiroCore. Se preferir
    /// gerar a sua, apague-a (e reverta o snapshot) e rode:
    /// dotnet ef migrations add ModuloFrota -p GestaoFrota.Infrastructure -s GestaoFrota.API
    /// </summary>
    [DbContext(typeof(GestaoFrota.Infrastructure.Data.AppDbContext))]
    [Migration("20261021120000_ModuloFrota")]
    public partial class ModuloFrota : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ---------- Veiculos ----------
            migrationBuilder.AddColumn<decimal>(
                name: "CapacidadeCargaToneladas",
                table: "Veiculos",
                type: "decimal(10,2)",
                nullable: false,
                defaultValue: 0m);

            // ---------- Viagens ----------
            migrationBuilder.AddColumn<decimal>(
                name: "QuilometragemRodadaKm",
                table: "Viagens",
                type: "decimal(12,2)",
                nullable: true);

            // Viagens já encerradas (se houver) ganham a quilometragem calculada.
            migrationBuilder.Sql(
                "UPDATE [Viagens] SET [QuilometragemRodadaKm] = [HodometroFinal] - [HodometroInicial] WHERE [HodometroFinal] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Viagens_VeiculoId_Status",
                table: "Viagens",
                columns: new[] { "VeiculoId", "Status" });

            // ---------- Movimentacoes ----------
            migrationBuilder.AddColumn<decimal>(
                name: "QuantidadeLitros",
                table: "Movimentacoes",
                type: "decimal(10,2)",
                nullable: true);

            // ---------- Manutencoes ----------
            migrationBuilder.CreateTable(
                name: "Manutencoes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VeiculoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Data = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Descricao = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    ValorTotal = table.Column<decimal>(type: "decimal(14,2)", nullable: false),
                    DataConclusao = table.Column<DateTime>(type: "datetime2", nullable: true),
                    MovimentacaoId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DataCriacao = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DataAtualizacao = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Manutencoes", x => x.Id);
                    table.CheckConstraint("CK_Manutencao_ValorPositivo", "[ValorTotal] > 0");
                    table.ForeignKey(
                        name: "FK_Manutencoes_Movimentacoes_MovimentacaoId",
                        column: x => x.MovimentacaoId,
                        principalTable: "Movimentacoes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Manutencoes_Veiculos_VeiculoId",
                        column: x => x.VeiculoId,
                        principalTable: "Veiculos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Manutencoes_MovimentacaoId",
                table: "Manutencoes",
                column: "MovimentacaoId");

            migrationBuilder.CreateIndex(
                name: "IX_Manutencoes_VeiculoId",
                table: "Manutencoes",
                column: "VeiculoId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "Manutencoes");

            migrationBuilder.DropColumn(name: "QuantidadeLitros", table: "Movimentacoes");

            migrationBuilder.DropIndex(name: "IX_Viagens_VeiculoId_Status", table: "Viagens");
            migrationBuilder.DropColumn(name: "QuilometragemRodadaKm", table: "Viagens");

            migrationBuilder.DropColumn(name: "CapacidadeCargaToneladas", table: "Veiculos");
        }
    }
}
