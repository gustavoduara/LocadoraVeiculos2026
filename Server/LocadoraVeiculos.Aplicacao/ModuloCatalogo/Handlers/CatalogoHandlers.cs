using FluentResults;
using LocadoraDeVeiculos.Aplicacao.Compartilhado;
using LocadoraDeVeiculos.Aplicacao.ModuloCatalogo.Commands;
using LocadoraDeVeiculos.Dominio.ModuloAluguel;
using LocadoraDeVeiculos.Infraestrutura.Orm.ModuloCatalogo;
using LocadoraDeVeiculos.Infraestrutura.Orm.ModuloReserva;
using MediatR;

namespace LocadoraDeVeiculos.Aplicacao.ModuloCatalogo.Handlers;

public class PesquisarCatalogoQueryHandler(
    RepositorioCatalogoEmOrm repositorio
) : IRequestHandler<PesquisarCatalogoQuery, Result<CatalogoResult>>
{
    private const int TamanhoMaximoDaPagina = 50;

    public async Task<Result<CatalogoResult>> Handle(PesquisarCatalogoQuery query, CancellationToken cancellationToken)
    {
        DateOnly hoje = DateOnly.FromDateTime(DateTime.UtcNow);

        if (query.Retirada is null != query.Devolucao is null)
            return Result.Fail(ResultadosErro.RequisicaoInvalidaErro("Informe a retirada e a devolução juntas."));

        if (query.Retirada is not null && query.Devolucao is not null)
        {
            // Uma folga de 1 dia: o servidor trabalha em UTC e o cliente pode estar um dia atrás.
            if (query.Retirada < hoje.AddDays(-1))
                return Result.Fail(ResultadosErro.RequisicaoInvalidaErro("A retirada não pode ser no passado."));

            if (query.Devolucao < query.Retirada)
                return Result.Fail(ResultadosErro.RequisicaoInvalidaErro("A devolução não pode ser antes da retirada."));
        }

        int pagina = Math.Max(1, query.Pagina);
        int tamanho = Math.Clamp(query.TamanhoPagina <= 0 ? 12 : query.TamanhoPagina, 1, TamanhoMaximoDaPagina);

        List<CarroDoCatalogo> carros = await repositorio.SelecionarCarrosAsync();

        carros = Filtrar(carros, query);

        int? dias = query.Retirada is { } r && query.Devolucao is { } d ? Aluguel.CalcularDias(r, d) : null;

        // Disponibilidade no período, quando há datas.
        HashSet<Guid> ocupados = [];

        if (dias is not null)
        {
            List<PeriodoOcupado> periodos = await repositorio.SelecionarPeriodosOcupadosAsync(carros.Select(c => c.Id).ToList());

            // Mesma regra do aluguel: um aluguel vencido e não devolvido continua ocupando o carro até hoje.
            ocupados = periodos
                .Where(p => p.Saida <= query.Devolucao!.Value &&
                            (p.RetornoPrevisto >= hoje ? p.RetornoPrevisto : hoje) >= query.Retirada!.Value)
                .Select(p => p.AutomovelId)
                .ToHashSet();
        }

        var itens = carros
            .Select(c => new CarroCatalogoDto(
                c.Id,
                c.LocadoraId,
                c.Locadora,
                c.Marca,
                c.Modelo,
                c.Cor,
                c.Ano,
                c.TipoCombustivel,
                c.Categoria,
                c.TemFoto,
                c.QuantidadeFotos,
                c.DiariaAPartirDe,
                c.DiariaAPartirDe is { } diaria && dias is { } n ? Math.Round(diaria * n, 2, MidpointRounding.AwayFromZero) : null,
                dias is null ? null : !ocupados.Contains(c.Id)))
            .ToList();

        if (query.ApenasDisponiveis && dias is not null)
            itens = itens.Where(i => i.Disponivel == true).ToList();

        itens = Ordenar(itens, query.Ordenacao, dias is not null);

        var paginaDeItens = itens.Skip((pagina - 1) * tamanho).Take(tamanho).ToList();

        return Result.Ok(new CatalogoResult(paginaDeItens, itens.Count, pagina, tamanho, dias));
    }

    private static List<CarroDoCatalogo> Filtrar(List<CarroDoCatalogo> carros, PesquisarCatalogoQuery q)
    {
        IEnumerable<CarroDoCatalogo> resultado = carros;

        if (!string.IsNullOrWhiteSpace(q.Texto))
        {
            string[] termos = q.Texto.Trim().ToLowerInvariant().Split(' ', StringSplitOptions.RemoveEmptyEntries);

            // Todos os termos precisam aparecer em algum campo (marca, modelo, cor, categoria ou locadora).
            resultado = resultado.Where(c =>
            {
                string alvo = $"{c.Marca} {c.Modelo} {c.Cor} {c.Categoria} {c.Locadora}".ToLowerInvariant();

                return termos.All(alvo.Contains);
            });
        }

        if (!string.IsNullOrWhiteSpace(q.Categoria))
            resultado = resultado.Where(c => c.Categoria.Equals(q.Categoria.Trim(), StringComparison.OrdinalIgnoreCase));

        if (q.Combustivel is not null)
            resultado = resultado.Where(c => c.TipoCombustivel == q.Combustivel);

        if (q.AnoMinimo is not null)
            resultado = resultado.Where(c => c.Ano >= q.AnoMinimo);

        if (q.LocadoraId is not null)
            resultado = resultado.Where(c => c.LocadoraId == q.LocadoraId);

        // Carro sem plano de cobrança não tem preço, então não entra num filtro de preço.
        if (q.DiariaMaxima is not null)
            resultado = resultado.Where(c => c.DiariaAPartirDe is { } d && d <= q.DiariaMaxima);

        return resultado.ToList();
    }

    private static List<CarroCatalogoDto> Ordenar(List<CarroCatalogoDto> itens, OrdenacaoCatalogo ordenacao, bool comDatas)
    {
        // Sem preço vai sempre para o fim. Com datas, os disponíveis vêm antes dos indisponíveis.
        IOrderedEnumerable<CarroCatalogoDto> ordenado = comDatas
            ? itens.OrderByDescending(i => i.Disponivel == true)
            : itens.OrderBy(_ => 0);

        ordenado = ordenacao switch
        {
            OrdenacaoCatalogo.MaiorPreco => ordenado.ThenBy(i => i.DiariaAPartirDe is null).ThenByDescending(i => i.DiariaAPartirDe),
            OrdenacaoCatalogo.MaisNovo => ordenado.ThenByDescending(i => i.Ano),
            OrdenacaoCatalogo.Marca => ordenado.ThenBy(i => i.Marca).ThenBy(i => i.Modelo),
            _ => ordenado.ThenBy(i => i.DiariaAPartirDe is null).ThenBy(i => i.DiariaAPartirDe)
        };

        return ordenado.ThenBy(i => i.Marca).ThenBy(i => i.Modelo).ThenBy(i => i.Id).ToList();
    }
}

public class FiltrosDoCatalogoQueryHandler(
    RepositorioCatalogoEmOrm repositorio
) : IRequestHandler<FiltrosDoCatalogoQuery, Result<FiltrosDoCatalogoResult>>
{
    public async Task<Result<FiltrosDoCatalogoResult>> Handle(FiltrosDoCatalogoQuery query, CancellationToken cancellationToken)
    {
        List<CarroDoCatalogo> carros = await repositorio.SelecionarCarrosAsync();

        return Result.Ok(new FiltrosDoCatalogoResult(
            carros.Select(c => c.Categoria).Distinct(StringComparer.OrdinalIgnoreCase).Order().ToList(),
            carros.Select(c => c.Marca).Distinct(StringComparer.OrdinalIgnoreCase).Order().ToList(),
            carros
                .GroupBy(c => c.LocadoraId)
                .Select(g => new LocadoraFiltroDto(g.Key, g.First().Locadora))
                .OrderBy(l => l.Nome)
                .ToList()));
    }
}

public class FotoDoCatalogoQueryHandler(
    RepositorioCatalogoEmOrm repositorio
) : IRequestHandler<FotoDoCatalogoQuery, Result<FotoDoCatalogoResult>>
{
    public async Task<Result<FotoDoCatalogoResult>> Handle(FotoDoCatalogoQuery query, CancellationToken cancellationToken)
    {
        FotoDoCarro? foto = await repositorio.SelecionarFotoAsync(query.AutomovelId, query.Posicao);

        if (foto is null)
            return Result.Fail(ResultadosErro.RegistroNaoEncontradoErro(query.AutomovelId));

        return Result.Ok(new FotoDoCatalogoResult(foto.Conteudo, foto.TipoConteudo));
    }
}

public class DetalharCarroQueryHandler(
    RepositorioReservaEmOrm repositorioReserva,
    RepositorioCatalogoEmOrm repositorioCatalogo
) : IRequestHandler<DetalharCarroQuery, Result<CarroDetalheDto>>
{
    public async Task<Result<CarroDetalheDto>> Handle(DetalharCarroQuery query, CancellationToken cancellationToken)
    {
        CarroReservavel? carro = await repositorioReserva.SelecionarCarroAsync(query.AutomovelId);

        if (carro is null)
            return Result.Fail(ResultadosErro.RegistroNaoEncontradoErro(query.AutomovelId));

        CarroDoCatalogo? basico = (await repositorioCatalogo.SelecionarCarrosAsync()).FirstOrDefault(c => c.Id == query.AutomovelId);

        if (basico is null)
            return Result.Fail(ResultadosErro.RegistroNaoEncontradoErro(query.AutomovelId));

        var plano = carro.Plano;

        var taxas = (await repositorioReserva.SelecionarTaxasDaLocadoraAsync(carro.EmpresaId))
            .Select(t => new TaxaOpcaoDto(t.Id, t.Nome, t.Preco, t.TipoCobranca))
            .ToList();

        return Result.Ok(new CarroDetalheDto(
            basico.Id,
            basico.LocadoraId,
            basico.Locadora,
            basico.Marca,
            basico.Modelo,
            basico.Cor,
            basico.Ano,
            basico.TipoCombustivel,
            basico.Categoria,
            basico.TemFoto,
            basico.QuantidadeFotos,
            plano is null ? null : new PlanoDiarioOpcaoDto(plano.DiarioPrecoDiaria, plano.DiarioPrecoPorKm),
            plano is null ? null : new PlanoControladoOpcaoDto(plano.ControladoKmDisponiveis, plano.ControladoPrecoDiaria, plano.ControladoPrecoPorKmExtrapolado),
            plano is null ? null : new PlanoLivreOpcaoDto(plano.LivrePrecoDiaria),
            carro.SeguroDiaria > 0 ? carro.SeguroDiaria : null,
            Aluguel.ValorCaucaoPadrao,
            taxas));
    }
}
