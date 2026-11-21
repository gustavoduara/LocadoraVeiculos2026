using FluentValidation;
using LocadoraDeVeiculos.Aplicacao.ModuloAluguel.Commands;

namespace LocadoraDeVeiculos.Aplicacao.ModuloAluguel.Validators;

/// <summary>Regras de formato dos dados do aluguel (as regras que dependem do banco ficam no montador).</summary>
internal class DadosAluguelValidator : AbstractValidator<DadosAluguelDto>
{
    private const int DiasMaximosDeAluguel = 365;

    public DadosAluguelValidator()
    {
        RuleFor(p => p.CondutorId).NotEmpty().WithMessage("O condutor é obrigatório.")
            .When(p => p.NovoCliente is null);
        RuleFor(p => p.CondutorId).Empty()
            .WithMessage("Escolha um condutor existente ou informe os dados de um cliente novo, não os dois.")
            .When(p => p.NovoCliente is not null);
        RuleFor(p => p.AutomovelId).NotEmpty().WithMessage("O automóvel é obrigatório.");

        RuleFor(p => p.TipoPlano).IsInEnum().WithMessage("O plano de cobrança informado é inválido.");
        RuleFor(p => p.TipoSeguro).IsInEnum().WithMessage("O tipo de seguro informado é inválido.");

        RuleFor(p => p.DataSaida)
            .Must(d => d != default).WithMessage("A data de saída é obrigatória.");

        RuleFor(p => p.DataRetornoPrevista)
            .Must(d => d != default).WithMessage("A data de retorno prevista é obrigatória.")
            .GreaterThanOrEqualTo(p => p.DataSaida)
            .WithMessage("A data de retorno prevista não pode ser anterior à data de saída.")
            .Must((p, retorno) => retorno.DayNumber - p.DataSaida.DayNumber <= DiasMaximosDeAluguel)
            .WithMessage($"O aluguel não pode passar de {DiasMaximosDeAluguel} dias.");

        RuleFor(p => p.KmInicial)
            .GreaterThanOrEqualTo(0).WithMessage("A quilometragem inicial não pode ser negativa.")
            .LessThanOrEqualTo(2_000_000).WithMessage("A quilometragem inicial informada é inválida.");

        RuleFor(p => p.TaxasIds)
            .NotNull().WithMessage("A lista de taxas e serviços é obrigatória (pode ser vazia).");
    }
}

internal class DadosDevolucaoValidator : AbstractValidator<DevolverAluguelCommand>
{
    public DadosDevolucaoValidator()
    {
        RuleFor(p => p.DataDevolucao).Must(d => d != default).WithMessage("A data de devolução é obrigatória.");

        RuleFor(p => p.KmFinal)
            .GreaterThanOrEqualTo(0).WithMessage("A quilometragem atual não pode ser negativa.")
            .LessThanOrEqualTo(2_000_000).WithMessage("A quilometragem atual informada é inválida.");
    }
}
