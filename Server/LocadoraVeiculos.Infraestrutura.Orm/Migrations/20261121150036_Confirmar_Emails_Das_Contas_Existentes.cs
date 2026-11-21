using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LocadoraDeVeiculos.Infraestrutura.Orm.Migrations
{
    /// <inheritdoc />
    public partial class Confirmar_Emails_Das_Contas_Existentes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // A confirmação de e-mail passou a ser exigida para entrar. Até aqui toda conta já nascia confirmada,
            // então isto é só uma garantia: nenhuma conta que já existia fica trancada fora do sistema.
            migrationBuilder.Sql("UPDATE AspNetUsers SET EmailConfirmed = 1 WHERE EmailConfirmed = 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Não há como saber quais contas já estavam confirmadas antes: não desfazemos.
        }
    }
}
