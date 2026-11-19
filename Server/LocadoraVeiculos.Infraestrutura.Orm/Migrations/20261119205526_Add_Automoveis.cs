using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LocadoraDeVeiculos.Infraestrutura.Orm.Migrations
{
    /// <inheritdoc />
    public partial class Add_Automoveis : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Automoveis",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Placa = table.Column<string>(type: "varchar(8)", nullable: false),
                    Marca = table.Column<string>(type: "nvarchar(50)", nullable: false),
                    Modelo = table.Column<string>(type: "nvarchar(50)", nullable: false),
                    Cor = table.Column<string>(type: "nvarchar(30)", nullable: false),
                    Ano = table.Column<int>(type: "int", nullable: false),
                    TipoCombustivel = table.Column<int>(type: "int", nullable: false),
                    CapacidadeCombustivelLitros = table.Column<int>(type: "int", nullable: false),
                    GrupoVeiculosId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Foto = table.Column<byte[]>(type: "varbinary(max)", nullable: true),
                    FotoTipoConteudo = table.Column<string>(type: "varchar(50)", nullable: true),
                    FotoNomeArquivo = table.Column<string>(type: "nvarchar(255)", nullable: true),
                    CriadoEmUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ExcluidoEmUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    Excluido = table.Column<bool>(type: "bit", nullable: false),
                    EmpresaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Automoveis", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Automoveis_AspNetUsers_EmpresaId",
                        column: x => x.EmpresaId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Automoveis_GruposVeiculos_GrupoVeiculosId",
                        column: x => x.GrupoVeiculosId,
                        principalTable: "GruposVeiculos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Automoveis_EmpresaId_Placa",
                table: "Automoveis",
                columns: new[] { "EmpresaId", "Placa" },
                unique: true,
                filter: "[Excluido] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Automoveis_GrupoVeiculosId",
                table: "Automoveis",
                column: "GrupoVeiculosId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Automoveis");
        }
    }
}
