using FluentResults;
using LocadoraDeVeiculos.Aplicacao.Compartilhado;
using LocadoraDeVeiculos.Dominio.ModuloAutenticacao;
using MediatR;

namespace LocadoraDeVeiculos.Aplicacao.ModuloConversa;

// ---------------- cliente ----------------

/// <summary>O cliente escreve para a locadora de um carro (pela tela do carro ou do pedido).</summary>
public record IniciarConversaCommand(Guid AutomovelId, string? Texto) : IRequest<Result<Guid>>;

public record EnviarMensagemDoClienteCommand(Guid ConversaId, string? Texto, Guid? AutomovelId) : IRequest<Result<MensagemDto>>;

public record ListarConversasDoClienteQuery() : IRequest<Result<IReadOnlyList<ConversaResumoDto>>>;

public record AbrirConversaDoClienteQuery(Guid ConversaId) : IRequest<Result<ConversaDetalheDto>>;

public record TotalNaoLidasDoClienteQuery() : IRequest<Result<int>>;

// ---------------- locadora ----------------

public record EnviarMensagemDaLocadoraCommand(Guid ConversaId, string? Texto) : IRequest<Result<MensagemDto>>;

public record ListarConversasDaLocadoraQuery() : IRequest<Result<IReadOnlyList<ConversaResumoDto>>>;

public record AbrirConversaDaLocadoraQuery(Guid ConversaId) : IRequest<Result<ConversaDetalheDto>>;

public record TotalNaoLidasDaLocadoraQuery() : IRequest<Result<int>>;

public class ConversaHandlers(ConversaServico servico, ITenantProvider tenantProvider) :
    IRequestHandler<IniciarConversaCommand, Result<Guid>>,
    IRequestHandler<EnviarMensagemDoClienteCommand, Result<MensagemDto>>,
    IRequestHandler<ListarConversasDoClienteQuery, Result<IReadOnlyList<ConversaResumoDto>>>,
    IRequestHandler<AbrirConversaDoClienteQuery, Result<ConversaDetalheDto>>,
    IRequestHandler<TotalNaoLidasDoClienteQuery, Result<int>>,
    IRequestHandler<EnviarMensagemDaLocadoraCommand, Result<MensagemDto>>,
    IRequestHandler<ListarConversasDaLocadoraQuery, Result<IReadOnlyList<ConversaResumoDto>>>,
    IRequestHandler<AbrirConversaDaLocadoraQuery, Result<ConversaDetalheDto>>,
    IRequestHandler<TotalNaoLidasDaLocadoraQuery, Result<int>>
{
    private Result Deslogado() => Result.Fail(ResultadosErro.NaoAutorizadoErro("Entre para continuar."));

    public async Task<Result<Guid>> Handle(IniciarConversaCommand c, CancellationToken ct) =>
        tenantProvider.UsuarioId is { } id
            ? await servico.IniciarComoClienteAsync(id, c.AutomovelId, c.Texto, ct)
            : Deslogado().ToResult<Guid>();

    public async Task<Result<MensagemDto>> Handle(EnviarMensagemDoClienteCommand c, CancellationToken ct) =>
        tenantProvider.UsuarioId is { } id
            ? await servico.EnviarComoClienteAsync(id, c.ConversaId, c.Texto, c.AutomovelId, ct)
            : Deslogado().ToResult<MensagemDto>();

    public async Task<Result<IReadOnlyList<ConversaResumoDto>>> Handle(ListarConversasDoClienteQuery q, CancellationToken ct) =>
        tenantProvider.UsuarioId is { } id
            ? Result.Ok<IReadOnlyList<ConversaResumoDto>>(await servico.ListarDoClienteAsync(id))
            : Deslogado().ToResult<IReadOnlyList<ConversaResumoDto>>();

    public async Task<Result<ConversaDetalheDto>> Handle(AbrirConversaDoClienteQuery q, CancellationToken ct) =>
        tenantProvider.UsuarioId is { } id
            ? await servico.AbrirComoClienteAsync(id, q.ConversaId, ct)
            : Deslogado().ToResult<ConversaDetalheDto>();

    public async Task<Result<int>> Handle(TotalNaoLidasDoClienteQuery q, CancellationToken ct) =>
        tenantProvider.UsuarioId is { } id
            ? Result.Ok(await servico.TotalNaoLidasDoClienteAsync(id))
            : Deslogado().ToResult<int>();

    public async Task<Result<MensagemDto>> Handle(EnviarMensagemDaLocadoraCommand c, CancellationToken ct) =>
        tenantProvider.UsuarioId is { } id
            ? await servico.EnviarComoLocadoraAsync(id, c.ConversaId, c.Texto, ct)
            : Deslogado().ToResult<MensagemDto>();

    public async Task<Result<IReadOnlyList<ConversaResumoDto>>> Handle(ListarConversasDaLocadoraQuery q, CancellationToken ct) =>
        Result.Ok<IReadOnlyList<ConversaResumoDto>>(await servico.ListarDaLocadoraAsync());

    public Task<Result<ConversaDetalheDto>> Handle(AbrirConversaDaLocadoraQuery q, CancellationToken ct) =>
        servico.AbrirComoLocadoraAsync(q.ConversaId, ct);

    public async Task<Result<int>> Handle(TotalNaoLidasDaLocadoraQuery q, CancellationToken ct) =>
        Result.Ok(await servico.TotalNaoLidasDaLocadoraAsync());
}
