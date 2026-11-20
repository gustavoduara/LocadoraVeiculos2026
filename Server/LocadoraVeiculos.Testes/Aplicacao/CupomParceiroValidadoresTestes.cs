using FluentValidation.TestHelper;
using LocadoraDeVeiculos.Aplicacao.ModuloCupom.Commands;
using LocadoraDeVeiculos.Aplicacao.ModuloCupom.Validators;
using LocadoraDeVeiculos.Aplicacao.ModuloParceiro.Commands;
using LocadoraDeVeiculos.Aplicacao.ModuloParceiro.Validators;

namespace LocadoraDeVeiculos.Testes.Aplicacao;

public class CupomParceiroValidadoresTestes
{
    private static readonly DateOnly Hoje = DateOnly.FromDateTime(DateTime.UtcNow);

    [Theory]
    [InlineData("Alfa", true)]
    [InlineData("A", false)]
    [InlineData("", false)]
    public void Parceiro_exige_nome_de_2_a_100_caracteres(string nome, bool valido)
    {
        var resultado = new CadastrarParceiroCommandValidator().TestValidate(new CadastrarParceiroCommand(nome));

        Assert.Equal(valido, resultado.IsValid);
    }

    [Fact]
    public void Parceiro_com_nome_gigante_e_recusado()
    {
        new EditarParceiroCommandValidator()
            .TestValidate(new EditarParceiroCommand(Guid.NewGuid(), new string('a', 101)))
            .ShouldHaveValidationErrorFor(c => c.Nome);
    }

    [Fact]
    public void Cupom_valido_passa()
    {
        new CadastrarCupomCommandValidator()
            .TestValidate(new CadastrarCupomCommand("PROMO10", 10m, Hoje.AddDays(30), Guid.NewGuid()))
            .ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    [InlineData(100000)]
    public void Cupom_com_valor_fora_da_faixa_e_recusado(decimal valor)
    {
        new CadastrarCupomCommandValidator()
            .TestValidate(new CadastrarCupomCommand("PROMO10", valor, Hoje.AddDays(1), Guid.NewGuid()))
            .ShouldHaveValidationErrorFor(c => c.Valor);
    }

    [Fact]
    public void Cupom_com_tres_casas_decimais_e_recusado()
    {
        new CadastrarCupomCommandValidator()
            .TestValidate(new CadastrarCupomCommand("PROMO10", 10.123m, Hoje.AddDays(1), Guid.NewGuid()))
            .ShouldHaveValidationErrorFor(c => c.Valor);
    }

    [Fact]
    public void Cupom_novo_nao_pode_nascer_vencido_mas_a_edicao_pode_encerra_lo()
    {
        var vencido = Hoje.AddDays(-1);

        new CadastrarCupomCommandValidator()
            .TestValidate(new CadastrarCupomCommand("PROMO10", 10m, vencido, Guid.NewGuid()))
            .ShouldHaveValidationErrorFor(c => c.DataValidade);

        new EditarCupomCommandValidator()
            .TestValidate(new EditarCupomCommand(Guid.NewGuid(), "PROMO10", 10m, vencido, Guid.NewGuid()))
            .ShouldNotHaveValidationErrorFor(c => c.DataValidade);
    }

    [Fact]
    public void Cupom_exige_parceiro_e_nome()
    {
        var resultado = new CadastrarCupomCommandValidator()
            .TestValidate(new CadastrarCupomCommand("ab", 10m, Hoje.AddDays(1), Guid.Empty));

        resultado.ShouldHaveValidationErrorFor(c => c.ParceiroId);
        resultado.ShouldHaveValidationErrorFor(c => c.Nome);
    }
}
