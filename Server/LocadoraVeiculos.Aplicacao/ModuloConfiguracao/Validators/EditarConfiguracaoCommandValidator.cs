using FluentValidation;
using LocadoraDeVeiculos.Aplicacao.ModuloConfiguracao.Commands;

namespace LocadoraDeVeiculos.Aplicacao.ModuloConfiguracao.Validators;

public class EditarConfiguracaoCommandValidator : AbstractValidator<EditarConfiguracaoCommand>
{
    public EditarConfiguracaoCommandValidator()
    {
        RuleFor(p => p.PrecoGasolina).PrecoDeCombustivel();
        RuleFor(p => p.PrecoGas).PrecoDeCombustivel();
        RuleFor(p => p.PrecoDiesel).PrecoDeCombustivel();
        RuleFor(p => p.PrecoAlcool).PrecoDeCombustivel();
    }
}

internal static class RegrasDePreco
{
    public static IRuleBuilderOptions<T, decimal> PrecoDeCombustivel<T>(
        this IRuleBuilder<T, decimal> regra)
    {
        return regra
            .GreaterThan(0).WithMessage("O campo {PropertyName} precisa conter um valor maior que 0.")
            .LessThan(1000).WithMessage("O campo {PropertyName} precisa conter um valor menor que 1000.")
            .PrecisionScale(18, 2, true).WithMessage("O campo {PropertyName} deve ter no máximo 2 casas decimais.");
    }
}
