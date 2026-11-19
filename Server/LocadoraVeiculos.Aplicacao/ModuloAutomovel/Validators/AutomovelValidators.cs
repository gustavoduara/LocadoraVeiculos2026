using FluentValidation;
using LocadoraDeVeiculos.Aplicacao.ModuloAutomovel.Commands;

namespace LocadoraDeVeiculos.Aplicacao.ModuloAutomovel.Validators;

public class CadastrarAutomovelCommandValidator : AbstractValidator<CadastrarAutomovelCommand>
{
    public CadastrarAutomovelCommandValidator()
    {
        RuleFor(p => p.Placa).PlacaValida();
        RuleFor(p => p.Marca).TextoObrigatorio("Marca", 2, 50);
        RuleFor(p => p.Modelo).TextoObrigatorio("Modelo", 1, 50);
        RuleFor(p => p.Cor).TextoObrigatorio("Cor", 3, 30);
        RuleFor(p => p.Ano).AnoValido();
        RuleFor(p => p.TipoCombustivel).IsInEnum().WithMessage("O tipo de combustível informado é inválido.");
        RuleFor(p => p.CapacidadeCombustivelLitros).CapacidadeValida();
        RuleFor(p => p.GrupoVeiculosId).NotEmpty().WithMessage("O grupo de veículos é obrigatório.");
    }
}

public class EditarAutomovelCommandValidator : AbstractValidator<EditarAutomovelCommand>
{
    public EditarAutomovelCommandValidator()
    {
        RuleFor(p => p.Placa).PlacaValida();
        RuleFor(p => p.Marca).TextoObrigatorio("Marca", 2, 50);
        RuleFor(p => p.Modelo).TextoObrigatorio("Modelo", 1, 50);
        RuleFor(p => p.Cor).TextoObrigatorio("Cor", 3, 30);
        RuleFor(p => p.Ano).AnoValido();
        RuleFor(p => p.TipoCombustivel).IsInEnum().WithMessage("O tipo de combustível informado é inválido.");
        RuleFor(p => p.CapacidadeCombustivelLitros).CapacidadeValida();
        RuleFor(p => p.GrupoVeiculosId).NotEmpty().WithMessage("O grupo de veículos é obrigatório.");
    }
}

internal static class RegrasDoAutomovel
{
    /// <summary>Aceita o padrão antigo (ABC-1234) e o Mercosul (ABC1D23), com ou sem hífen.</summary>
    public static IRuleBuilderOptions<T, string> PlacaValida<T>(this IRuleBuilder<T, string> regra)
    {
        return regra
            .NotEmpty().WithMessage("A placa é obrigatória.")
            .Matches(@"^[A-Za-z]{3}-?\d[A-Za-z0-9]\d{2}$")
            .WithMessage("A placa deve seguir o padrão ABC-1234 ou ABC1D23.");
    }

    public static IRuleBuilderOptions<T, string> TextoObrigatorio<T>(
        this IRuleBuilder<T, string> regra, string campo, int minimo, int maximo)
    {
        return regra
            .NotEmpty().WithMessage($"O campo {campo} é obrigatório.")
            .MinimumLength(minimo).WithMessage($"O campo {campo} deve conter no mínimo {minimo} caracteres.")
            .MaximumLength(maximo).WithMessage($"O campo {campo} deve conter no máximo {maximo} caracteres.");
    }

    public static IRuleBuilderOptions<T, int> AnoValido<T>(this IRuleBuilder<T, int> regra)
    {
        return regra
            .GreaterThanOrEqualTo(1950).WithMessage("O ano do veículo deve ser a partir de 1950.")
            .LessThanOrEqualTo(DateTime.UtcNow.Year + 1)
            .WithMessage($"O ano do veículo deve ser no máximo {DateTime.UtcNow.Year + 1}.");
    }

    public static IRuleBuilderOptions<T, int> CapacidadeValida<T>(this IRuleBuilder<T, int> regra)
    {
        return regra
            .GreaterThan(0).WithMessage("A capacidade do tanque precisa ser maior que 0 litros.")
            .LessThanOrEqualTo(1000).WithMessage("A capacidade do tanque precisa ser de no máximo 1000 litros.");
    }
}
