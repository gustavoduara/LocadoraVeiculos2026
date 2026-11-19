using FluentValidation;
using LocadoraDeVeiculos.Aplicacao.Compartilhado;
using LocadoraDeVeiculos.Aplicacao.ModuloCondutor.Commands;

namespace LocadoraDeVeiculos.Aplicacao.ModuloCondutor.Validators;

public class CadastrarCondutorCommandValidator : AbstractValidator<CadastrarCondutorCommand>
{
    public CadastrarCondutorCommandValidator()
    {
        RuleFor(p => p.ClienteId).NotEmpty().WithMessage("O cliente é obrigatório.");
        RuleFor(p => p.Nome).NomeDoCondutor();
        RuleFor(p => p.Email).EmailDoCondutor();
        RuleFor(p => p.Telefone).TelefoneDoCondutor();
        RuleFor(p => p.Cpf).CpfDoCondutor();
        RuleFor(p => p.Cnh).CnhDoCondutor();
        RuleFor(p => p.ValidadeCnh).ValidadeDaCnh();
    }
}

public class EditarCondutorCommandValidator : AbstractValidator<EditarCondutorCommand>
{
    public EditarCondutorCommandValidator()
    {
        RuleFor(p => p.Nome).NomeDoCondutor();
        RuleFor(p => p.Email).EmailDoCondutor();
        RuleFor(p => p.Telefone).TelefoneDoCondutor();
        RuleFor(p => p.Cpf).CpfDoCondutor();
        RuleFor(p => p.Cnh).CnhDoCondutor();
        RuleFor(p => p.ValidadeCnh).ValidadeDaCnh();
    }
}

internal static class RegrasDoCondutor
{
    public static IRuleBuilderOptions<T, string> NomeDoCondutor<T>(this IRuleBuilder<T, string> regra) => regra
        .NotEmpty().WithMessage("O campo Nome é obrigatório.")
        .MinimumLength(3).WithMessage("O campo Nome deve conter no mínimo 3 caracteres.")
        .MaximumLength(150).WithMessage("O campo Nome deve conter no máximo 150 caracteres.");

    public static IRuleBuilderOptions<T, string> EmailDoCondutor<T>(this IRuleBuilder<T, string> regra) => regra
        .NotEmpty().WithMessage("O campo E-mail é obrigatório.")
        .EmailAddress().WithMessage("O campo E-mail deve conter um endereço válido.")
        .MaximumLength(150).WithMessage("O campo E-mail deve conter no máximo 150 caracteres.");

    public static IRuleBuilderOptions<T, string> TelefoneDoCondutor<T>(this IRuleBuilder<T, string> regra) => regra
        .NotEmpty().WithMessage("O campo Telefone é obrigatório.")
        .Matches(@"^\(\d{2}\) \d{4,5}-\d{4}$")
        .WithMessage("O campo Telefone deve seguir o formato '(00) 00000-0000' ou '(00) 0000-0000'.");

    public static IRuleBuilderOptions<T, string> CpfDoCondutor<T>(this IRuleBuilder<T, string> regra) => regra
        .NotEmpty().WithMessage("O campo CPF é obrigatório.")
        .Matches(@"^\d{3}\.\d{3}\.\d{3}-\d{2}$").WithMessage("O campo CPF deve seguir o formato '000.000.000-00'.")
        .Must(DocumentosUtil.CpfValido).WithMessage("O CPF informado é inválido.");

    public static IRuleBuilderOptions<T, string> CnhDoCondutor<T>(this IRuleBuilder<T, string> regra) => regra
        .NotEmpty().WithMessage("O campo CNH é obrigatório.")
        .Matches(@"^\d{11}$").WithMessage("O campo CNH deve conter 11 dígitos.");

    /// <summary>
    /// A data precisa ser plausível. Uma CNH vencida pode ser cadastrada (o condutor renova depois),
    /// mas o aluguel exige CNH em dia.
    /// </summary>
    public static IRuleBuilderOptions<T, DateOnly> ValidadeDaCnh<T>(this IRuleBuilder<T, DateOnly> regra) => regra
        .Must(data => data != default).WithMessage("A validade da CNH é obrigatória.")
        .Must(data => data >= new DateOnly(1990, 1, 1))
        .WithMessage("A validade da CNH informada é inválida.")
        .Must(data => data <= DateOnly.FromDateTime(DateTime.UtcNow).AddYears(15))
        .WithMessage("A validade da CNH não pode ser superior a 15 anos a partir de hoje.");
}
