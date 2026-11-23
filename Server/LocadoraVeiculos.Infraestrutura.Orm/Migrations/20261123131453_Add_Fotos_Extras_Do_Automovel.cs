using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LocadoraDeVeiculos.Infraestrutura.Orm.Migrations
{
    /// <inheritdoc />
    public partial class Add_Fotos_Extras_Do_Automovel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AutomovelFotos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AutomovelId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Ordem = table.Column<int>(type: "int", nullable: false),
                    Conteudo = table.Column<byte[]>(type: "varbinary(max)", nullable: false),
                    TipoConteudo = table.Column<string>(type: "varchar(50)", nullable: false),
                    NomeArquivo = table.Column<string>(type: "nvarchar(255)", nullable: false),
                    CriadaEmUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AutomovelFotos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AutomovelFotos_Automoveis_AutomovelId",
                        column: x => x.AutomovelId,
                        principalTable: "Automoveis",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AutomovelFotos_AutomovelId_Ordem",
                table: "AutomovelFotos",
                columns: new[] { "AutomovelId", "Ordem" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AutomovelFotos");
        }
    }
}
