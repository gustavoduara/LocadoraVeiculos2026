using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LocadoraDeVeiculos.Infraestrutura.Orm.Migrations
{
    /// <inheritdoc />
    public partial class Add_Conversas_E_Mensagens : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Conversas",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UsuarioClienteId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ClienteNome = table.Column<string>(type: "nvarchar(150)", nullable: false),
                    NomeLocadora = table.Column<string>(type: "nvarchar(150)", nullable: false),
                    UltimaMensagemEmUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UltimaMensagemResumo = table.Column<string>(type: "nvarchar(200)", nullable: false),
                    NaoLidasCliente = table.Column<int>(type: "int", nullable: false),
                    NaoLidasLocadora = table.Column<int>(type: "int", nullable: false),
                    CriadoEmUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ExcluidoEmUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    Excluido = table.Column<bool>(type: "bit", nullable: false),
                    EmpresaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Conversas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Conversas_AspNetUsers_EmpresaId",
                        column: x => x.EmpresaId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Mensagens",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ConversaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Autor = table.Column<int>(type: "int", nullable: false),
                    AutorNome = table.Column<string>(type: "nvarchar(150)", nullable: false),
                    Texto = table.Column<string>(type: "nvarchar(1000)", nullable: false),
                    EnviadaEmUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    AutomovelId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DescricaoVeiculo = table.Column<string>(type: "nvarchar(150)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Mensagens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Mensagens_Conversas_ConversaId",
                        column: x => x.ConversaId,
                        principalTable: "Conversas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Conversas_EmpresaId_UsuarioClienteId",
                table: "Conversas",
                columns: new[] { "EmpresaId", "UsuarioClienteId" },
                unique: true,
                filter: "[Excluido] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Conversas_UsuarioClienteId_UltimaMensagemEmUtc",
                table: "Conversas",
                columns: new[] { "UsuarioClienteId", "UltimaMensagemEmUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_Mensagens_ConversaId_EnviadaEmUtc",
                table: "Mensagens",
                columns: new[] { "ConversaId", "EnviadaEmUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Mensagens");

            migrationBuilder.DropTable(
                name: "Conversas");
        }
    }
}
