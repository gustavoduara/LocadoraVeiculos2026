using FluentValidation;
using LocadoraDeVeiculos.Aplicacao.ModuloPlanoCobranca.Commands;

namespace LocadoraDeVeiculos.Aplicacao.ModuloPlanoCobranca.Validators;

public class CadastrarPlanoCobrancaCommandValidator : AbstractValidator<CadastrarPlanoCobrancaCommand>
{
    public CadastrarPlanoCobrancaCommandValidator()
    {
        RuleFor(p => p.AutomovelId)
            .NotEmpty().WithMessage("O automóvel é obrigatório.");

        RuleFor(p => p.Diario).NotNull().WithMessage("Os valores do Plano Diário são obrigatórios.");
        RuleFor(p => p.Controlado).NotNull().WithMessage("Os valores do Plano Controlado são obrigatórios.");
        RuleFor(p => p.Livre).NotNull().WithMessage("Os valores do Plano Livre são obrigatórios.");

        RuleFor(p => p.Diario.PrecoDiaria).ValorMonetario("Preço da diária (Plano Diário)").When(p => p.Diario is not null);
        RuleFor(p => p.Diario.PrecoPorKm).ValorMonetario("Preço por km (Plano Diário)").When(p => p.Diario is not null);

        RuleFor(p => p.Controlado.KmDisponiveis).KmDisponiveis().When(p => p.Controlado is not null);
        RuleFor(p => p.Controlado.PrecoDiaria).ValorMonetario("Preço da diária (Plano Controlado)").When(p => p.Controlado is not null);
        RuleFor(p => p.Controlado.PrecoPorKmExtrapolado).ValorMonetario("Preço por km extrapolado (Plano Controlado)").When(p => p.Controlado is not null);

        RuleFor(p => p.Livre.PrecoDiaria).ValorMonetario("Preço da diária (Plano Livre)").When(p => p.Livre is not null);
    }
}

public class EditarPlanoCobrancaCommandValidator : AbstractValidator<EditarPlanoCobrancaCommand>
{
    public EditarPlanoCobrancaCommandValidator()
    {
        RuleFor(p => p.Diario).NotNull().WithMessage("Os valores do Plano Diário são obrigatórios.");
        RuleFor(p => p.Controlado).NotNull().WithMessage("Os valores do Plano Controlado são obrigatórios.");
        RuleFor(p => p.Livre).NotNull().WithMessage("Os valores do Plano Livre são obrigatórios.");

        RuleFor(p => p.Diario.PrecoDiaria).ValorMonetario("Preço da diária (Plano Diário)").When(p => p.Diario is not null);
        RuleFor(p => p.Diario.PrecoPorKm).ValorMonetario("Preço por km (Plano Diário)").When(p => p.Diario is not null);

        RuleFor(p => p.Controlado.KmDisponiveis).KmDisponiveis().When(p => p.Controlado is not null);
        RuleFor(p => p.Controlado.PrecoDiaria).ValorMonetario("Preço da diária (Plano Controlado)").When(p => p.Controlado is not null);
        RuleFor(p => p.Controlado.PrecoPorKmExtrapolado).ValorMonetario("Preço por km extrapolado (Plano Controlado)").When(p => p.Controlado is not null);

        RuleFor(p => p.Livre.PrecoDiaria).ValorMonetario("Preço da diária (Plano Livre)").When(p => p.Livre is not null);
    }
}

internal static class RegrasDosPlanos
{
    public static IRuleBuilderOptions<T, decimal> ValorMonetario<T>(
        this IRuleBuilder<T, decimal> regra, string campo)
    {
        return regra
            .GreaterThan(0).WithMessage($"O campo {campo} precisa conter um valor maior que 0.")
            .LessThanOrEqualTo(99_999.99m).WithMessage($"O campo {campo} precisa conter um valor de no máximo 99999,99.")
            .PrecisionScale(18, 2, true).WithMessage($"O campo {campo} deve ter no máximo 2 casas decimais.");
    }

    public static IRuleBuilderOptions<T, int> KmDisponiveis<T>(this IRuleBuilder<T, int> regra)
    {
        return regra
            .GreaterThan(0).WithMessage("Os km disponíveis (Plano Controlado) precisam ser maior que 0.")
            .LessThanOrEqualTo(100_000).WithMessage("Os km disponíveis (Plano Controlado) precisam ser no máximo 100000.");
    }
}
