using FluentValidation;
using LocadoraDeVeiculos.Aplicacao.Compartilhado;
using LocadoraDeVeiculos.Aplicacao.ModuloCliente.Commands;

namespace LocadoraDeVeiculos.Aplicacao.ModuloCliente.Validators;

// Estes validadores são internos de propósito: os handlers escolhem o correto pelo tipo do cliente
// (o registro automático por IValidator<DadosClienteDto> ficaria ambíguo).

/// <summary>Regras comuns a pessoa física e jurídica: contato e endereço.</summary>
internal abstract class DadosClienteBaseValidator : AbstractValidator<DadosClienteDto>
{
    protected DadosClienteBaseValidator()
    {
        RuleFor(p => p.Nome)
            .NotEmpty().WithMessage("O campo Nome é obrigatório.")
            .MinimumLength(3).WithMessage("O campo Nome deve conter no mínimo 3 caracteres.")
            .MaximumLength(150).WithMessage("O campo Nome deve conter no máximo 150 caracteres.");

        RuleFor(p => p.Email)
            .NotEmpty().WithMessage("O campo E-mail é obrigatório.")
            .EmailAddress().WithMessage("O campo E-mail deve conter um endereço válido.")
            .MaximumLength(150).WithMessage("O campo E-mail deve conter no máximo 150 caracteres.");

        RuleFor(p => p.Telefone)
            .NotEmpty().WithMessage("O campo Telefone é obrigatório.")
            .Matches(@"^\(\d{2}\) \d{4,5}-\d{4}$")
            .WithMessage("O campo Telefone deve seguir o formato '(00) 00000-0000' ou '(00) 0000-0000'.");

        RuleFor(p => p.Estado)
            .NotEmpty().WithMessage("O campo Estado é obrigatório.")
            .Matches(@"^[A-Za-z]{2}$").WithMessage("O campo Estado deve conter a sigla da UF com 2 letras.");

        RuleFor(p => p.Cidade)
            .NotEmpty().WithMessage("O campo Cidade é obrigatório.")
            .MinimumLength(2).WithMessage("O campo Cidade deve conter no mínimo 2 caracteres.")
            .MaximumLength(100).WithMessage("O campo Cidade deve conter no máximo 100 caracteres.");

        RuleFor(p => p.Bairro)
            .NotEmpty().WithMessage("O campo Bairro é obrigatório.")
            .MinimumLength(2).WithMessage("O campo Bairro deve conter no mínimo 2 caracteres.")
            .MaximumLength(100).WithMessage("O campo Bairro deve conter no máximo 100 caracteres.");

        RuleFor(p => p.Rua)
            .NotEmpty().WithMessage("O campo Rua é obrigatório.")
            .MinimumLength(2).WithMessage("O campo Rua deve conter no mínimo 2 caracteres.")
            .MaximumLength(150).WithMessage("O campo Rua deve conter no máximo 150 caracteres.");

        RuleFor(p => p.Numero)
            .NotEmpty().WithMessage("O campo Número é obrigatório.")
            .MaximumLength(10).WithMessage("O campo Número deve conter no máximo 10 caracteres.");
    }
}

internal class PessoaFisicaDadosValidator : DadosClienteBaseValidator
{
    public PessoaFisicaDadosValidator()
    {
        RuleFor(p => p.Cpf)
            .NotEmpty().WithMessage("O campo CPF é obrigatório.")
            .Matches(@"^\d{3}\.\d{3}\.\d{3}-\d{2}$").WithMessage("O campo CPF deve seguir o formato '000.000.000-00'.")
            .Must(DocumentosUtil.CpfValido).WithMessage("O CPF informado é inválido.");

        RuleFor(p => p.Rg)
            .NotEmpty().WithMessage("O campo RG é obrigatório.")
            .MinimumLength(5).WithMessage("O campo RG deve conter no mínimo 5 caracteres.")
            .MaximumLength(20).WithMessage("O campo RG deve conter no máximo 20 caracteres.");

        RuleFor(p => p.Cnh)
            .NotEmpty().WithMessage("O campo CNH é obrigatório.")
            .Matches(@"^\d{11}$").WithMessage("O campo CNH deve conter 11 dígitos.");
    }
}

internal class PessoaJuridicaDadosValidator : DadosClienteBaseValidator
{
    public PessoaJuridicaDadosValidator()
    {
        RuleFor(p => p.Cnpj)
            .NotEmpty().WithMessage("O campo CNPJ é obrigatório.")
            .Matches(@"^\d{2}\.\d{3}\.\d{3}/\d{4}-\d{2}$").WithMessage("O campo CNPJ deve seguir o formato '00.000.000/0000-00'.")
            .Must(DocumentosUtil.CnpjValido).WithMessage("O CNPJ informado é inválido.");
    }
}
