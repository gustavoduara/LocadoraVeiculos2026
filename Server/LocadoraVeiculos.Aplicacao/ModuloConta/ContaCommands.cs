using FluentResults;
using LocadoraDeVeiculos.Aplicacao.Compartilhado;
using LocadoraDeVeiculos.Dominio.ModuloAutenticacao;
using MediatR;

namespace LocadoraDeVeiculos.Aplicacao.ModuloConta;

/// <summary>O cliente exclui a própria conta, na hora: pede a senha e a palavra EXCLUIR.</summary>
public record ExcluirMinhaContaCommand(string? Senha, string? Confirmacao) : IRequest<Result>;

/// <summary>O administrador da locadora agenda a exclusão da empresa: pede a senha e o nome da empresa.</summary>
public record AgendarExclusaoCommand(string? Senha, string? NomeDaEmpresa) : IRequest<Result<DateTime>>;

public record CancelarExclusaoCommand() : IRequest<Result>;

/// <summary>Se a conta de quem está logado tem uma exclusão agendada (a tela mostra o aviso).</summary>
public record ObterSituacaoDaContaQuery() : IRequest<Result<SituacaoDaContaDto>>;

public record SituacaoDaContaDto(DateTime? ExclusaoAgendadaParaUtc);

public class ExcluirMinhaContaCommandHandler(ContaServico servico, ITenantProvider tenantProvider)
    : IRequestHandler<ExcluirMinhaContaCommand, Result>
{
    public async Task<Result> Handle(ExcluirMinhaContaCommand command, CancellationToken cancellationToken)
    {
        if (tenantProvider.UsuarioId is not { } usuarioId)
            return Result.Fail(ResultadosErro.NaoAutorizadoErro("Entre para continuar."));

        return await servico.ExcluirContaDeClienteAsync(usuarioId, command.Senha, command.Confirmacao, cancellationToken);
    }
}

public class AgendarExclusaoCommandHandler(ContaServico servico, ITenantProvider tenantProvider)
    : IRequestHandler<AgendarExclusaoCommand, Result<DateTime>>
{
    public async Task<Result<DateTime>> Handle(AgendarExclusaoCommand command, CancellationToken cancellationToken)
    {
        if (tenantProvider.UsuarioId is not { } usuarioId)
            return Result.Fail(ResultadosErro.NaoAutorizadoErro("Entre para continuar."));

        return await servico.AgendarExclusaoDaLocadoraAsync(usuarioId, command.Senha, command.NomeDaEmpresa, cancellationToken);
    }
}

public class CancelarExclusaoCommandHandler(ContaServico servico, ITenantProvider tenantProvider)
    : IRequestHandler<CancelarExclusaoCommand, Result>
{
    public async Task<Result> Handle(CancelarExclusaoCommand command, CancellationToken cancellationToken)
    {
        if (tenantProvider.UsuarioId is not { } usuarioId)
            return Result.Fail(ResultadosErro.NaoAutorizadoErro("Entre para continuar."));

        return await servico.CancelarExclusaoDaLocadoraAsync(usuarioId, cancellationToken);
    }
}

public class ObterSituacaoDaContaQueryHandler(ContaServico servico, ITenantProvider tenantProvider)
    : IRequestHandler<ObterSituacaoDaContaQuery, Result<SituacaoDaContaDto>>
{
    public async Task<Result<SituacaoDaContaDto>> Handle(ObterSituacaoDaContaQuery query, CancellationToken cancellationToken)
    {
        if (tenantProvider.UsuarioId is not { } usuarioId)
            return Result.Fail(ResultadosErro.NaoAutorizadoErro("Entre para continuar."));

        return Result.Ok(new SituacaoDaContaDto(await servico.ObterExclusaoAgendadaAsync(usuarioId)));
    }
}
