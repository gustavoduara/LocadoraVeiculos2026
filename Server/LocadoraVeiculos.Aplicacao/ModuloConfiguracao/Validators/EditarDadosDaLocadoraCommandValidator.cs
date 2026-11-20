using FluentValidation;
using LocadoraDeVeiculos.Aplicacao.ModuloConfiguracao.Commands;

namespace LocadoraDeVeiculos.Aplicacao.ModuloConfiguracao.Validators;

public class EditarDadosDaLocadoraCommandValidator : AbstractValidator<EditarDadosDaLocadoraCommand>
{
    public EditarDadosDaLocadoraCommandValidator()
    {
        RuleFor(p => p.NomeFantasia)
            .MinimumLength(2).WithMessage("O nome da locadora deve conter no mínimo 2 caracteres.")
            .MaximumLength(100).WithMessage("O nome da locadora deve conter no máximo 100 caracteres.")
            .When(p => !string.IsNullOrWhiteSpace(p.NomeFantasia));

        RuleFor(p => p.Telefone)
            .Matches(@"^\(\d{2}\) \d{4,5}-\d{4}$")
            .WithMessage("O telefone deve seguir o formato '(00) 00000-0000' ou '(00) 0000-0000'.")
            .When(p => !string.IsNullOrWhiteSpace(p.Telefone));
    }
}
