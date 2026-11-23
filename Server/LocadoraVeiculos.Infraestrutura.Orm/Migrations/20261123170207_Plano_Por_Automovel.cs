using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LocadoraDeVeiculos.Infraestrutura.Orm.Migrations
{
    /// <inheritdoc />
    public partial class Plano_Por_Automovel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PlanosCobranca_GruposVeiculos_GrupoVeiculosId",
                table: "PlanosCobranca");

            migrationBuilder.RenameColumn(
                name: "GrupoVeiculosId",
                table: "PlanosCobranca",
                newName: "AutomovelId");


            // Até aqui a coluna AutomovelId ainda guarda o id do GRUPO. Os planos existentes viram planos dos carros:
            // o plano original fica com o primeiro carro do grupo e os demais carros ganham uma cópia.
            migrationBuilder.Sql(@"
                INSERT INTO PlanosCobranca
                    (Id, AutomovelId, ControladoKmDisponiveis, ControladoPrecoDiaria, ControladoPrecoPorKmExtrapolado, CriadoEmUtc,
                     DiarioPrecoDiaria, DiarioPrecoPorKm, EmpresaId, Excluido, ExcluidoEmUtc, LivrePrecoDiaria)
                SELECT NEWID(), a.Id, p.ControladoKmDisponiveis, p.ControladoPrecoDiaria, p.ControladoPrecoPorKmExtrapolado, p.CriadoEmUtc,
                       p.DiarioPrecoDiaria, p.DiarioPrecoPorKm, p.EmpresaId, 0, NULL, p.LivrePrecoDiaria
                FROM PlanosCobranca p
                INNER JOIN Automoveis a ON a.GrupoVeiculosId = p.AutomovelId AND a.Excluido = 0
                WHERE p.Excluido = 0
                  AND a.Id <> (SELECT TOP 1 a2.Id FROM Automoveis a2
                               WHERE a2.GrupoVeiculosId = p.AutomovelId AND a2.Excluido = 0 ORDER BY a2.Id)");

            migrationBuilder.Sql(@"
                UPDATE p SET p.AutomovelId =
                    (SELECT TOP 1 a.Id FROM Automoveis a WHERE a.GrupoVeiculosId = p.AutomovelId AND a.Excluido = 0 ORDER BY a.Id)
                FROM PlanosCobranca p
                WHERE p.AutomovelId IN (SELECT Id FROM GruposVeiculos)
                  AND EXISTS (SELECT 1 FROM Automoveis a WHERE a.GrupoVeiculosId = p.AutomovelId AND a.Excluido = 0)");

            // Planos de grupos que não têm carro: os usados por algum aluguel ficam (excluídos) presos ao carro do aluguel; o resto sai.
            migrationBuilder.Sql(@"
                UPDATE p SET p.Excluido = 1, p.ExcluidoEmUtc = SYSUTCDATETIME(),
                    p.AutomovelId = (SELECT TOP 1 al.AutomovelId FROM Alugueis al WHERE al.PlanoCobrancaId = p.Id)
                FROM PlanosCobranca p
                WHERE p.AutomovelId NOT IN (SELECT Id FROM Automoveis)
                  AND EXISTS (SELECT 1 FROM Alugueis al WHERE al.PlanoCobrancaId = p.Id)");

            migrationBuilder.Sql("DELETE FROM PlanosCobranca WHERE AutomovelId NOT IN (SELECT Id FROM Automoveis)");

            migrationBuilder.RenameIndex(
                name: "IX_PlanosCobranca_GrupoVeiculosId",
                table: "PlanosCobranca",
                newName: "IX_PlanosCobranca_AutomovelId");

            migrationBuilder.RenameIndex(
                name: "IX_PlanosCobranca_EmpresaId_GrupoVeiculosId",
                table: "PlanosCobranca",
                newName: "IX_PlanosCobranca_EmpresaId_AutomovelId");

            migrationBuilder.AddForeignKey(
                name: "FK_PlanosCobranca_Automoveis_AutomovelId",
                table: "PlanosCobranca",
                column: "AutomovelId",
                principalTable: "Automoveis",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PlanosCobranca_Automoveis_AutomovelId",
                table: "PlanosCobranca");

            migrationBuilder.RenameColumn(
                name: "AutomovelId",
                table: "PlanosCobranca",
                newName: "GrupoVeiculosId");

            migrationBuilder.RenameIndex(
                name: "IX_PlanosCobranca_EmpresaId_AutomovelId",
                table: "PlanosCobranca",
                newName: "IX_PlanosCobranca_EmpresaId_GrupoVeiculosId");

            migrationBuilder.RenameIndex(
                name: "IX_PlanosCobranca_AutomovelId",
                table: "PlanosCobranca",
                newName: "IX_PlanosCobranca_GrupoVeiculosId");

            migrationBuilder.AddForeignKey(
                name: "FK_PlanosCobranca_GruposVeiculos_GrupoVeiculosId",
                table: "PlanosCobranca",
                column: "GrupoVeiculosId",
                principalTable: "GruposVeiculos",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
