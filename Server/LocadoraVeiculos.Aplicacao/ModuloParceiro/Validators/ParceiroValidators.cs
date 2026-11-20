using FluentValidation;
using LocadoraDeVeiculos.Aplicacao.ModuloParceiro.Commands;

namespace LocadoraDeVeiculos.Aplicacao.ModuloParceiro.Validators;

public class CadastrarParceiroCommandValidator : AbstractValidator<CadastrarParceiroCommand>
{
    public CadastrarParceiroCommandValidator()
    {
        RuleFor(p => p.Nome).NomeDoParceiro();
    }
}

public class EditarParceiroCommandValidator : AbstractValidator<EditarParceiroCommand>
{
    public EditarParceiroCommandValidator()
    {
        RuleFor(p => p.Nome).NomeDoParceiro();
    }
}

internal static class RegrasDoParceiro
{
    public static IRuleBuilderOptions<T, string> NomeDoParceiro<T>(this IRuleBuilder<T, string> regra)
    {
        return regra
            .NotEmpty().WithMessage("O campo Nome é obrigatório.")
            .MinimumLength(2).WithMessage("O campo Nome deve conter no mínimo 2 caracteres.")
            .MaximumLength(100).WithMessage("O campo Nome deve conter no máximo 100 caracteres.");
    }
}
