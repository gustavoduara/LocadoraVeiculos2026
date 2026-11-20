using FluentValidation;
using LocadoraDeVeiculos.Aplicacao.ModuloCupom.Commands;

namespace LocadoraDeVeiculos.Aplicacao.ModuloCupom.Validators;

public class CadastrarCupomCommandValidator : AbstractValidator<CadastrarCupomCommand>
{
    public CadastrarCupomCommandValidator()
    {
        RuleFor(p => p.Nome).NomeDoCupom();
        RuleFor(p => p.Valor).ValorDoCupom();
        RuleFor(p => p.ParceiroId).NotEmpty().WithMessage("O parceiro é obrigatório.");

        // Um cupom novo não nasce vencido (ao editar, a data pode ser antecipada para encerrá-lo).
        RuleFor(p => p.DataValidade)
            .Must(d => d != default).WithMessage("A data de validade é obrigatória.")
            .Must(d => d >= DateOnly.FromDateTime(DateTime.UtcNow))
            .WithMessage("A data de validade não pode estar no passado.");
    }
}

public class EditarCupomCommandValidator : AbstractValidator<EditarCupomCommand>
{
    public EditarCupomCommandValidator()
    {
        RuleFor(p => p.Nome).NomeDoCupom();
        RuleFor(p => p.Valor).ValorDoCupom();
        RuleFor(p => p.ParceiroId).NotEmpty().WithMessage("O parceiro é obrigatório.");
        RuleFor(p => p.DataValidade).Must(d => d != default).WithMessage("A data de validade é obrigatória.");
    }
}

internal static class RegrasDoCupom
{
    public static IRuleBuilderOptions<T, string> NomeDoCupom<T>(this IRuleBuilder<T, string> regra)
    {
        return regra
            .NotEmpty().WithMessage("O campo Nome é obrigatório.")
            .MinimumLength(3).WithMessage("O campo Nome deve conter no mínimo 3 caracteres.")
            .MaximumLength(50).WithMessage("O campo Nome deve conter no máximo 50 caracteres.");
    }

    public static IRuleBuilderOptions<T, decimal> ValorDoCupom<T>(this IRuleBuilder<T, decimal> regra)
    {
        return regra
            .GreaterThan(0).WithMessage("O campo Valor precisa conter um valor maior que 0.")
            .LessThanOrEqualTo(99_999.99m).WithMessage("O campo Valor precisa conter um valor de no máximo 99999,99.")
            .PrecisionScale(18, 2, true).WithMessage("O campo Valor deve ter no máximo 2 casas decimais.");
    }
}
