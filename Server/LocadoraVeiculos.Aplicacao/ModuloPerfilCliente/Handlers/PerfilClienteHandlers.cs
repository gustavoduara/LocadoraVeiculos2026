using FluentResults;
using FluentValidation;
using LocadoraDeVeiculos.Aplicacao.Compartilhado;
using LocadoraDeVeiculos.Aplicacao.ModuloPerfilCliente.Commands;
using LocadoraDeVeiculos.Dominio.ModuloAutenticacao;
using LocadoraDeVeiculos.Dominio.ModuloPerfilCliente;
using LocadoraDeVeiculos.Infraestrutura.Orm.Compartilhado;
using LocadoraDeVeiculos.Infraestrutura.Orm.ModuloPerfilCliente;
using MediatR;
using Microsoft.Extensions.Logging;

namespace LocadoraDeVeiculos.Aplicacao.ModuloPerfilCliente.Handlers;

internal static class PerfilClienteMapeamento
{
    public static PerfilClienteDto ParaDto(PerfilCliente? p)
    {
        if (p is null)
            return new PerfilClienteDto(false, "", "", "", null, false, "", "", "", "", "", "");

        bool vencida = p.ValidadeCnh < DateOnly.FromDateTime(DateTime.UtcNow);

        return new PerfilClienteDto(
            true, p.Cpf, p.Rg, p.Cnh, p.ValidadeCnh, vencida, p.Telefone, p.Estado, p.Cidade, p.Bairro, p.Rua, p.Numero);
    }
}

/// <summary>As mesmas regras de documento e endereço do cadastro de cliente (pessoa física).</summary>
public class DadosPerfilClienteValidator : AbstractValidator<DadosPerfilClienteDto>
{
    public DadosPerfilClienteValidator()
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

        RuleFor(p => p.ValidadeCnh)
            .Must(d => d != default).WithMessage("A validade da CNH é obrigatória.");

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

public class SelecionarMeusDadosQueryHandler(
    RepositorioPerfilClienteEmOrm repositorio,
    ITenantProvider tenantProvider
) : IRequestHandler<SelecionarMeusDadosQuery, Result<PerfilClienteDto>>
{
    public async Task<Result<PerfilClienteDto>> Handle(SelecionarMeusDadosQuery query, CancellationToken cancellationToken)
    {
        if (tenantProvider.UsuarioId is not { } usuarioId)
            return Result.Fail(ResultadosErro.RegistroNaoEncontradoErro("Usuário não identificado."));

        return Result.Ok(PerfilClienteMapeamento.ParaDto(await repositorio.SelecionarAsync(usuarioId)));
    }
}

public class SalvarMeusDadosCommandHandler(
    AppDbContext appDbContext,
    RepositorioPerfilClienteEmOrm repositorio,
    ITenantProvider tenantProvider,
    ILogger<SalvarMeusDadosCommandHandler> logger
) : IRequestHandler<SalvarMeusDadosCommand, Result<PerfilClienteDto>>
{
    public async Task<Result<PerfilClienteDto>> Handle(SalvarMeusDadosCommand command, CancellationToken cancellationToken)
    {
        if (tenantProvider.UsuarioId is not { } usuarioId)
            return Result.Fail(ResultadosErro.RegistroNaoEncontradoErro("Usuário não identificado."));

        var validacao = new DadosPerfilClienteValidator().Validate(command.Dados);

        if (!validacao.IsValid)
            return Result.Fail(ResultadosErro.RequisicaoInvalidaErro(validacao.Errors.Select(e => e.ErrorMessage)));

        try
        {
            DadosPerfilClienteDto d = command.Dados;

            var novo = new PerfilCliente
            {
                UsuarioId = usuarioId,
                Cpf = d.Cpf,
                Rg = d.Rg.Trim(),
                Cnh = d.Cnh,
                ValidadeCnh = d.ValidadeCnh,
                Telefone = d.Telefone,
                Estado = d.Estado.Trim().ToUpperInvariant(),
                Cidade = d.Cidade.Trim(),
                Bairro = d.Bairro.Trim(),
                Rua = d.Rua.Trim(),
                Numero = d.Numero.Trim()
            };

            PerfilCliente? existente = await repositorio.SelecionarAsync(usuarioId);

            if (existente is null)
                await repositorio.CadastrarAsync(novo);
            else
                existente.Atualizar(novo);

            await appDbContext.SaveChangesAsync(cancellationToken);

            return Result.Ok(PerfilClienteMapeamento.ParaDto(existente ?? novo));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ocorreu um erro ao salvar os dados do cliente.");

            return Result.Fail(ResultadosErro.ExcecaoInternaErro(ex));
        }
    }
}
