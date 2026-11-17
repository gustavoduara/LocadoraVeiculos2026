using FluentValidation;
using LocadoraDeVeiculos.Aplicacao.ModuloFuncionario.Commands;

namespace LocadoraDeVeiculos.Aplicacao.ModuloFuncionario.Validators;

public class EditarMeuPerfilCommandValidator : AbstractValidator<EditarMeuPerfilCommand>
{
    public EditarMeuPerfilCommandValidator()
    {
        RuleFor(p => p.NomeCompleto)
            .NotEmpty().WithMessage("O campo {PropertyName} é obrigatório.")
            .MinimumLength(3).WithMessage("O campo {PropertyName} deve conter no mínimo {MinLength} caracteres.");

        RuleFor(p => p.Cpf)
            .NotEmpty().WithMessage("O campo {PropertyName} é obrigatório.")
            .Matches(@"^\d{3}\.\d{3}\.\d{3}-\d{2}$")
            .WithMessage("O campo {PropertyName} deve seguir o formato '000.000.000-00'.");
    }
}
