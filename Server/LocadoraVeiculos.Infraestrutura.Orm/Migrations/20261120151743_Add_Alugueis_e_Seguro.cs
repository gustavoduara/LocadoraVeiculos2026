using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LocadoraDeVeiculos.Infraestrutura.Orm.Migrations
{
    /// <inheritdoc />
    public partial class Add_Alugueis_e_Seguro : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "SeguroClienteDiaria",
                table: "Configuracoes",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "SeguroTerceirosDiaria",
                table: "Configuracoes",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateTable(
                name: "Alugueis",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CondutorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ClienteId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AutomovelId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PlanoCobrancaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FuncionarioId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    NomeCondutor = table.Column<string>(type: "nvarchar(150)", nullable: false),
                    NomeCliente = table.Column<string>(type: "nvarchar(150)", nullable: false),
                    DescricaoVeiculo = table.Column<string>(type: "nvarchar(150)", nullable: false),
                    NomeGrupo = table.Column<string>(type: "nvarchar(100)", nullable: false),
                    DataSaida = table.Column<DateOnly>(type: "date", nullable: false),
                    DataRetornoPrevista = table.Column<DateOnly>(type: "date", nullable: false),
                    KmInicial = table.Column<int>(type: "int", nullable: false),
                    TipoPlano = table.Column<int>(type: "int", nullable: false),
                    PlanoPrecoDiaria = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    PlanoPrecoPorKm = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    PlanoKmDisponiveisPorDia = table.Column<int>(type: "int", nullable: false),
                    TipoSeguro = table.Column<int>(type: "int", nullable: false),
                    SeguroPrecoDiaria = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ValorCaucao = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    DataDevolucao = table.Column<DateOnly>(type: "date", nullable: true),
                    KmFinal = table.Column<int>(type: "int", nullable: true),
                    TanqueCheio = table.Column<bool>(type: "bit", nullable: true),
                    PrecoCombustivelPorLitro = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    CapacidadeTanqueLitros = table.Column<int>(type: "int", nullable: true),
                    CriadoEmUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ExcluidoEmUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    Excluido = table.Column<bool>(type: "bit", nullable: false),
                    EmpresaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Alugueis", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Alugueis_AspNetUsers_EmpresaId",
                        column: x => x.EmpresaId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Alugueis_Automoveis_AutomovelId",
                        column: x => x.AutomovelId,
                        principalTable: "Automoveis",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Alugueis_Clientes_ClienteId",
                        column: x => x.ClienteId,
                        principalTable: "Clientes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Alugueis_Condutores_CondutorId",
                        column: x => x.CondutorId,
                        principalTable: "Condutores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Alugueis_Funcionarios_FuncionarioId",
                        column: x => x.FuncionarioId,
                        principalTable: "Funcionarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Alugueis_PlanosCobranca_PlanoCobrancaId",
                        column: x => x.PlanoCobrancaId,
                        principalTable: "PlanosCobranca",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AluguelTaxa",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AluguelId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TaxaServicoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Nome = table.Column<string>(type: "nvarchar(100)", nullable: false),
                    PrecoUnitario = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TipoCobranca = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AluguelTaxa", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AluguelTaxa_Alugueis_AluguelId",
                        column: x => x.AluguelId,
                        principalTable: "Alugueis",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AluguelTaxa_TaxasServicos_TaxaServicoId",
                        column: x => x.TaxaServicoId,
                        principalTable: "TaxasServicos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Alugueis_AutomovelId",
                table: "Alugueis",
                column: "AutomovelId");

            migrationBuilder.CreateIndex(
                name: "IX_Alugueis_ClienteId",
                table: "Alugueis",
                column: "ClienteId");

            migrationBuilder.CreateIndex(
                name: "IX_Alugueis_CondutorId",
                table: "Alugueis",
                column: "CondutorId");

            migrationBuilder.CreateIndex(
                name: "IX_Alugueis_EmpresaId_AutomovelId_Status",
                table: "Alugueis",
                columns: new[] { "EmpresaId", "AutomovelId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Alugueis_FuncionarioId",
                table: "Alugueis",
                column: "FuncionarioId");

            migrationBuilder.CreateIndex(
                name: "IX_Alugueis_PlanoCobrancaId",
                table: "Alugueis",
                column: "PlanoCobrancaId");

            migrationBuilder.CreateIndex(
                name: "IX_AluguelTaxa_AluguelId",
                table: "AluguelTaxa",
                column: "AluguelId");

            migrationBuilder.CreateIndex(
                name: "IX_AluguelTaxa_TaxaServicoId",
                table: "AluguelTaxa",
                column: "TaxaServicoId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AluguelTaxa");

            migrationBuilder.DropTable(
                name: "Alugueis");

            migrationBuilder.DropColumn(
                name: "SeguroClienteDiaria",
                table: "Configuracoes");

            migrationBuilder.DropColumn(
                name: "SeguroTerceirosDiaria",
                table: "Configuracoes");
        }
    }
}
