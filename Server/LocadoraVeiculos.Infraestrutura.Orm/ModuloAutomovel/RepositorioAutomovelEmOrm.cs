using LocadoraDeVeiculos.Dominio.Compartilhado;
using LocadoraDeVeiculos.Dominio.ModuloAutomovel;
using LocadoraDeVeiculos.Infraestrutura.Orm.Compartilhado;
using Microsoft.EntityFrameworkCore;

namespace LocadoraDeVeiculos.Infraestrutura.Orm.ModuloAutomovel;

/// <summary>Projeção leve para listagens: não carrega os bytes da foto.</summary>
public record AutomovelResumo(
    Guid Id,
    string Placa,
    string Marca,
    string Modelo,
    string Cor,
    int Ano,
    TipoCombustivel TipoCombustivel,
    int CapacidadeCombustivelLitros,
    Guid GrupoVeiculosId,
    string NomeGrupo,
    bool TemFoto,
    string? FotoNomeArquivo
);

public class RepositorioAutomovelEmOrm : RepositorioBaseEmOrm<Automovel>
{
    private readonly AppDbContext dbContext;

    public RepositorioAutomovelEmOrm(AppDbContext dbContext) : base(dbContext)
    {
        this.dbContext = dbContext;
    }

    public override async Task<Automovel?> SelecionarPorIdAsync(Guid idRegistro)
    {
        return await dbContext.Automoveis
            .Include(a => a.GrupoVeiculos)
            .FirstOrDefaultAsync(a => a.Id == idRegistro);
    }

    public async Task<List<AutomovelResumo>> SelecionarResumosAsync(Guid? grupoVeiculosId = null)
    {
        IQueryable<Automovel> consulta = dbContext.Automoveis;

        if (grupoVeiculosId is not null)
            consulta = consulta.Where(a => a.GrupoVeiculosId == grupoVeiculosId);

        return await consulta
            .OrderBy(a => a.GrupoVeiculos!.Nome)
            .ThenBy(a => a.Placa)
            .Select(a => new AutomovelResumo(
                a.Id,
                a.Placa,
                a.Marca,
                a.Modelo,
                a.Cor,
                a.Ano,
                a.TipoCombustivel,
                a.CapacidadeCombustivelLitros,
                a.GrupoVeiculosId,
                a.GrupoVeiculos!.Nome,
                a.Foto != null,
                a.FotoNomeArquivo
            ))
            .ToListAsync();
    }

    public async Task<bool> ExistePlacaAsync(string placa, Guid? ignorarId = null)
    {
        return await dbContext.Automoveis
            .AnyAsync(a => a.Placa == placa && (ignorarId == null || a.Id != ignorarId));
    }

    public async Task<bool> ExisteParaGrupoAsync(Guid grupoVeiculosId)
    {
        return await dbContext.Automoveis
            .AnyAsync(a => a.GrupoVeiculosId == grupoVeiculosId);
    }
}
