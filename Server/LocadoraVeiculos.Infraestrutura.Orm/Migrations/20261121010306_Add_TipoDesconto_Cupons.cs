using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LocadoraDeVeiculos.Infraestrutura.Orm.Migrations
{
    /// <inheritdoc />
    public partial class Add_TipoDesconto_Cupons : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Tipo",
                table: "Cupons",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "CupomTipo",
                table: "Alugueis",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Tipo",
                table: "Cupons");

            migrationBuilder.DropColumn(
                name: "CupomTipo",
                table: "Alugueis");
        }
    }
}
