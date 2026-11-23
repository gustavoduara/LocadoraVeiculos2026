using LocadoraDeVeiculos.Dominio.Compartilhado;
using LocadoraDeVeiculos.Dominio.ModuloAluguel;
using LocadoraDeVeiculos.Infraestrutura.Orm.Compartilhado;
using Microsoft.EntityFrameworkCore;

namespace LocadoraDeVeiculos.Infraestrutura.Orm.ModuloCatalogo;

/// <summary>Um carro de qualquer locadora, com os dados que o catálogo mostra (sem a placa).</summary>
public record CarroDoCatalogo(
    Guid Id,
    Guid LocadoraId,
    string Locadora,
    string Marca,
    string Modelo,
    string Cor,
    int Ano,
    TipoCombustivel TipoCombustivel,
    Guid GrupoVeiculosId,
    string Categoria,
    bool TemFoto,
    /// <summary>Total de fotos (capa + extras).</summary>
    int QuantidadeFotos,
    /// <summary>Menor diária entre os três planos do grupo; vazio se o grupo não tem plano de cobrança.</summary>
    decimal? DiariaAPartirDe
);

public record PeriodoOcupado(Guid AutomovelId, DateOnly Saida, DateOnly RetornoPrevisto);

public record LocadoraDoCatalogo(Guid Id, string Nome);

public record FotoDoCarro(byte[] Conteudo, string TipoConteudo);

/// <summary>
/// Leituras do catálogo, que atravessam as locadoras: por isso ignoram o filtro por empresa
/// (mas respeitam a exclusão lógica) e só devolvem dados que o catálogo pode mostrar.
/// </summary>
public class RepositorioCatalogoEmOrm(AppDbContext dbContext)
{
    public async Task<List<CarroDoCatalogo>> SelecionarCarrosAsync()
    {
        var carros = await dbContext.Automoveis
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(a => !a.Excluido && !a.GrupoVeiculos!.Excluido)
            .Select(a => new
            {
                a.Id,
                a.EmpresaId,
                a.Marca,
                a.Modelo,
                a.Cor,
                a.Ano,
                a.TipoCombustivel,
                a.GrupoVeiculosId,
                Categoria = a.GrupoVeiculos!.Nome,
                TemCapa = a.Foto != null,
                Extras = a.FotosExtras.Count
            })
            .ToListAsync();

        var planos = await dbContext.PlanosCobranca
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(p => !p.Excluido)
            .Select(p => new
            {
                p.AutomovelId,
                Menor = Math.Min(p.DiarioPrecoDiaria, Math.Min(p.ControladoPrecoDiaria, p.LivrePrecoDiaria))
            })
            .ToListAsync();

        var menorDiariaPorCarro = planos.ToDictionary(p => p.AutomovelId, p => p.Menor);

        var nomes = await NomesDasLocadorasAsync(carros.Select(c => c.EmpresaId).Distinct().ToList());

        return carros
            .Select(c => new CarroDoCatalogo(
                c.Id,
                c.EmpresaId,
                nomes.GetValueOrDefault(c.EmpresaId, "Locadora"),
                c.Marca,
                c.Modelo,
                c.Cor,
                c.Ano,
                c.TipoCombustivel,
                c.GrupoVeiculosId,
                c.Categoria,
                c.TemCapa || c.Extras > 0,
                (c.TemCapa ? 1 : 0) + c.Extras,
                menorDiariaPorCarro.TryGetValue(c.Id, out var menor) ? menor : null))
            .ToList();
    }

    /// <summary>Aluguéis em aberto (de qualquer locadora) que ocupam os carros informados.</summary>
    public async Task<List<PeriodoOcupado>> SelecionarPeriodosOcupadosAsync(IReadOnlyCollection<Guid> automovelIds)
    {
        return await dbContext.Alugueis
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(a => !a.Excluido && a.Status == StatusAluguel.EmAberto && automovelIds.Contains(a.AutomovelId))
            .Select(a => new PeriodoOcupado(a.AutomovelId, a.DataSaida, a.DataRetornoPrevista))
            .ToListAsync();
    }

    /// <summary>
    /// A foto na posição pedida da galeria do carro: 0 é a capa (se houver) e depois vêm as extras, na ordem.
    /// Sem capa, a primeira extra ocupa a posição 0.
    /// </summary>
    public async Task<FotoDoCarro?> SelecionarFotoAsync(Guid automovelId, int posicao = 0)
    {
        if (posicao < 0) return null;

        var capa = await dbContext.Automoveis
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(a => a.Id == automovelId && !a.Excluido)
            .Select(a => new { TemCapa = a.Foto != null })
            .FirstOrDefaultAsync();

        if (capa is null) return null;

        if (capa.TemCapa && posicao == 0)
        {
            var principal = await dbContext.Automoveis
                .IgnoreQueryFilters()
                .AsNoTracking()
                .Where(a => a.Id == automovelId)
                .Select(a => new { a.Foto, a.FotoTipoConteudo })
                .FirstAsync();

            return new FotoDoCarro(principal.Foto!, principal.FotoTipoConteudo ?? "application/octet-stream");
        }

        int indiceExtra = capa.TemCapa ? posicao - 1 : posicao;

        var extra = await dbContext.AutomovelFotos
            .AsNoTracking()
            .Where(f => f.AutomovelId == automovelId)
            .OrderBy(f => f.Ordem).ThenBy(f => f.CriadaEmUtc)
            .Skip(indiceExtra)
            .Take(1)
            .Select(f => new { f.Conteudo, f.TipoConteudo })
            .FirstOrDefaultAsync();

        return extra is null ? null : new FotoDoCarro(extra.Conteudo, extra.TipoConteudo);
    }

    public async Task<string> SelecionarNomeDaLocadoraAsync(Guid empresaId)
    {
        var nomes = await NomesDasLocadorasAsync([empresaId]);

        return nomes.GetValueOrDefault(empresaId, "Locadora");
    }

    private async Task<Dictionary<Guid, string>> NomesDasLocadorasAsync(IReadOnlyCollection<Guid> empresaIds)
    {
        // A locadora é a conta "Empresa": o id da empresa é o id do usuário. O nome é o nome fantasia (se a locadora
        // informou) ou, senão, o nome da conta.
        var nomesDasContas = await dbContext.Users
            .AsNoTracking()
            .Where(u => empresaIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => u.FullName);

        var fantasias = await dbContext.Configuracoes
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(c => !c.Excluido && empresaIds.Contains(c.EmpresaId) && c.NomeFantasia != null)
            .ToDictionaryAsync(c => c.EmpresaId, c => c.NomeFantasia!);

        return nomesDasContas.ToDictionary(n => n.Key, n => fantasias.GetValueOrDefault(n.Key, n.Value));
    }
}
