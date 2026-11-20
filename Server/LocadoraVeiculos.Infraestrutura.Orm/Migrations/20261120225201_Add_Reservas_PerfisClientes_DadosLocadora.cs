using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LocadoraDeVeiculos.Infraestrutura.Orm.Migrations
{
    /// <inheritdoc />
    public partial class Add_Reservas_PerfisClientes_DadosLocadora : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "NomeFantasia",
                table: "Configuracoes",
                type: "nvarchar(100)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Telefone",
                table: "Configuracoes",
                type: "nvarchar(20)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "PerfisClientes",
                columns: table => new
                {
                    UsuarioId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Cpf = table.Column<string>(type: "nvarchar(14)", nullable: false),
                    Rg = table.Column<string>(type: "nvarchar(20)", nullable: false),
                    Cnh = table.Column<string>(type: "nvarchar(11)", nullable: false),
                    ValidadeCnh = table.Column<DateOnly>(type: "date", nullable: false),
                    Telefone = table.Column<string>(type: "nvarchar(20)", nullable: false),
                    Estado = table.Column<string>(type: "nvarchar(2)", nullable: false),
                    Cidade = table.Column<string>(type: "nvarchar(100)", nullable: false),
                    Bairro = table.Column<string>(type: "nvarchar(100)", nullable: false),
                    Rua = table.Column<string>(type: "nvarchar(150)", nullable: false),
                    Numero = table.Column<string>(type: "nvarchar(10)", nullable: false),
                    AtualizadoEmUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PerfisClientes", x => x.UsuarioId);
                    table.ForeignKey(
                        name: "FK_PerfisClientes_AspNetUsers_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Reservas",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UsuarioClienteId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AutomovelId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NomeLocadora = table.Column<string>(type: "nvarchar(150)", nullable: false),
                    DescricaoVeiculo = table.Column<string>(type: "nvarchar(150)", nullable: false),
                    NomeGrupo = table.Column<string>(type: "nvarchar(100)", nullable: false),
                    Retirada = table.Column<DateOnly>(type: "date", nullable: false),
                    Devolucao = table.Column<DateOnly>(type: "date", nullable: false),
                    TipoPlano = table.Column<int>(type: "int", nullable: false),
                    TipoSeguro = table.Column<int>(type: "int", nullable: false),
                    Dias = table.Column<int>(type: "int", nullable: false),
                    ValorDiarias = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ValorSeguro = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ValorCaucao = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ValorTotalPrevisto = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ClienteNome = table.Column<string>(type: "nvarchar(150)", nullable: false),
                    ClienteEmail = table.Column<string>(type: "nvarchar(256)", nullable: false),
                    ClienteTelefone = table.Column<string>(type: "nvarchar(20)", nullable: false),
                    ClienteCpf = table.Column<string>(type: "nvarchar(14)", nullable: false),
                    ClienteRg = table.Column<string>(type: "nvarchar(20)", nullable: false),
                    ClienteCnh = table.Column<string>(type: "nvarchar(11)", nullable: false),
                    ClienteValidadeCnh = table.Column<DateOnly>(type: "date", nullable: false),
                    ClienteEstado = table.Column<string>(type: "nvarchar(2)", nullable: false),
                    ClienteCidade = table.Column<string>(type: "nvarchar(100)", nullable: false),
                    ClienteBairro = table.Column<string>(type: "nvarchar(100)", nullable: false),
                    ClienteRua = table.Column<string>(type: "nvarchar(150)", nullable: false),
                    ClienteNumero = table.Column<string>(type: "nvarchar(10)", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    MotivoRecusa = table.Column<string>(type: "nvarchar(300)", nullable: true),
                    AluguelId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RespondidaEmUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CriadoEmUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ExcluidoEmUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    Excluido = table.Column<bool>(type: "bit", nullable: false),
                    EmpresaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Reservas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Reservas_Alugueis_AluguelId",
                        column: x => x.AluguelId,
                        principalTable: "Alugueis",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Reservas_AspNetUsers_EmpresaId",
                        column: x => x.EmpresaId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Reservas_Automoveis_AutomovelId",
                        column: x => x.AutomovelId,
                        principalTable: "Automoveis",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Reservas_AluguelId",
                table: "Reservas",
                column: "AluguelId");

            migrationBuilder.CreateIndex(
                name: "IX_Reservas_AutomovelId",
                table: "Reservas",
                column: "AutomovelId");

            migrationBuilder.CreateIndex(
                name: "IX_Reservas_EmpresaId_Status",
                table: "Reservas",
                columns: new[] { "EmpresaId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Reservas_UsuarioClienteId_Status",
                table: "Reservas",
                columns: new[] { "UsuarioClienteId", "Status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PerfisClientes");

            migrationBuilder.DropTable(
                name: "Reservas");

            migrationBuilder.DropColumn(
                name: "NomeFantasia",
                table: "Configuracoes");

            migrationBuilder.DropColumn(
                name: "Telefone",
                table: "Configuracoes");
        }
    }
}
