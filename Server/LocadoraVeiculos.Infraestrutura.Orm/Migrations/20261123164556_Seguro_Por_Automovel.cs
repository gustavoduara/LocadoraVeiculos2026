using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LocadoraDeVeiculos.Infraestrutura.Orm.Migrations
{
    /// <inheritdoc />
    public partial class Seguro_Por_Automovel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "SeguroDiaria",
                table: "Automoveis",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            // Os carros que já existem herdam o valor do "seguro do cliente" que a locadora tinha configurado.
            migrationBuilder.Sql(@"
                UPDATE a SET a.SeguroDiaria = c.SeguroClienteDiaria
                FROM Automoveis a
                INNER JOIN Configuracoes c ON c.EmpresaId = a.EmpresaId AND c.Excluido = 0");

            // O seguro "para terceiros" deixou de existir: aluguéis e reservas antigos passam a contar como "com seguro".
            migrationBuilder.Sql("UPDATE Alugueis SET TipoSeguro = 1 WHERE TipoSeguro = 2");
            migrationBuilder.Sql("UPDATE Reservas SET TipoSeguro = 1 WHERE TipoSeguro = 2");

            migrationBuilder.DropColumn(
                name: "SeguroClienteDiaria",
                table: "Configuracoes");

            migrationBuilder.DropColumn(
                name: "SeguroTerceirosDiaria",
                table: "Configuracoes");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SeguroDiaria",
                table: "Automoveis");

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
        }
    }
}
