using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LocadoraDeVeiculos.Infraestrutura.Orm.Migrations
{
    /// <inheritdoc />
    public partial class Add_Clientes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Clientes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Tipo = table.Column<int>(type: "int", nullable: false),
                    Nome = table.Column<string>(type: "nvarchar(150)", nullable: false),
                    Email = table.Column<string>(type: "varchar(150)", nullable: false),
                    Telefone = table.Column<string>(type: "varchar(15)", nullable: false),
                    Cpf = table.Column<string>(type: "varchar(14)", nullable: true),
                    Rg = table.Column<string>(type: "varchar(20)", nullable: true),
                    Cnh = table.Column<string>(type: "varchar(11)", nullable: true),
                    Cnpj = table.Column<string>(type: "varchar(18)", nullable: true),
                    Estado = table.Column<string>(type: "varchar(2)", nullable: false),
                    Cidade = table.Column<string>(type: "nvarchar(100)", nullable: false),
                    Bairro = table.Column<string>(type: "nvarchar(100)", nullable: false),
                    Rua = table.Column<string>(type: "nvarchar(150)", nullable: false),
                    Numero = table.Column<string>(type: "varchar(10)", nullable: false),
                    PessoaJuridicaId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CriadoEmUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ExcluidoEmUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    Excluido = table.Column<bool>(type: "bit", nullable: false),
                    EmpresaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Clientes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Clientes_AspNetUsers_EmpresaId",
                        column: x => x.EmpresaId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Clientes_Clientes_PessoaJuridicaId",
                        column: x => x.PessoaJuridicaId,
                        principalTable: "Clientes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Clientes_EmpresaId_Cnpj",
                table: "Clientes",
                columns: new[] { "EmpresaId", "Cnpj" },
                unique: true,
                filter: "[Excluido] = 0 AND [Cnpj] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Clientes_EmpresaId_Cpf",
                table: "Clientes",
                columns: new[] { "EmpresaId", "Cpf" },
                unique: true,
                filter: "[Excluido] = 0 AND [Cpf] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Clientes_PessoaJuridicaId",
                table: "Clientes",
                column: "PessoaJuridicaId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Clientes");
        }
    }
}
