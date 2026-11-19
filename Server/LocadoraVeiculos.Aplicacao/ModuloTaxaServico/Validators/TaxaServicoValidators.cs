using FluentValidation;
using LocadoraDeVeiculos.Aplicacao.ModuloTaxaServico.Commands;

namespace LocadoraDeVeiculos.Aplicacao.ModuloTaxaServico.Validators;

public class CadastrarTaxaServicoCommandValidator : AbstractValidator<CadastrarTaxaServicoCommand>
{
    public CadastrarTaxaServicoCommandValidator()
    {
        RuleFor(p => p.Nome).NomeDaTaxa();
        RuleFor(p => p.Preco).PrecoDaTaxa();
        RuleFor(p => p.TipoCobranca).IsInEnum().WithMessage("O tipo de cobrança informado é inválido.");
    }
}

public class EditarTaxaServicoCommandValidator : AbstractValidator<EditarTaxaServicoCommand>
{
    public EditarTaxaServicoCommandValidator()
    {
        RuleFor(p => p.Nome).NomeDaTaxa();
        RuleFor(p => p.Preco).PrecoDaTaxa();
        RuleFor(p => p.TipoCobranca).IsInEnum().WithMessage("O tipo de cobrança informado é inválido.");
    }
}

internal static class RegrasDaTaxa
{
    public static IRuleBuilderOptions<T, string> NomeDaTaxa<T>(this IRuleBuilder<T, string> regra)
    {
        return regra
            .NotEmpty().WithMessage("O campo Nome é obrigatório.")
            .MinimumLength(3).WithMessage("O campo Nome deve conter no mínimo 3 caracteres.")
            .MaximumLength(100).WithMessage("O campo Nome deve conter no máximo 100 caracteres.");
    }

    public static IRuleBuilderOptions<T, decimal> PrecoDaTaxa<T>(this IRuleBuilder<T, decimal> regra)
    {
        return regra
            .GreaterThan(0).WithMessage("O campo Preço precisa conter um valor maior que 0.")
            .LessThanOrEqualTo(99_999.99m).WithMessage("O campo Preço precisa conter um valor de no máximo 99999,99.")
            .PrecisionScale(18, 2, true).WithMessage("O campo Preço deve ter no máximo 2 casas decimais.");
    }
}
