using LocadoraDeVeiculos.Dominio.ModuloTaxaServico;
using LocadoraDeVeiculos.Infraestrutura.Orm.Compartilhado;
using Microsoft.EntityFrameworkCore;

namespace LocadoraDeVeiculos.Infraestrutura.Orm.ModuloTaxaServico;

public class RepositorioTaxaServicoEmOrm : RepositorioBaseEmOrm<TaxaServico>
{
    private readonly AppDbContext dbContext;

    public RepositorioTaxaServicoEmOrm(AppDbContext dbContext) : base(dbContext)
    {
        this.dbContext = dbContext;
    }

    public override async Task<List<TaxaServico>> SelecionarTodosAsync()
    {
        return await dbContext.TaxasServicos
            .OrderBy(t => t.Nome)
            .ToListAsync();
    }

    public async Task<List<TaxaServico>> SelecionarPorIdsAsync(IReadOnlyCollection<Guid> ids)
    {
        return await dbContext.TaxasServicos
            .Where(t => ids.Contains(t.Id))
            .ToListAsync();
    }

    /// <summary>Usa a collation do banco (sem diferenciar maiúsculas/minúsculas), igual ao índice único.</summary>
    public async Task<bool> ExisteNomeAsync(string nome, Guid? ignorarId = null)
    {
        return await dbContext.TaxasServicos
            .AnyAsync(t => t.Nome == nome && (ignorarId == null || t.Id != ignorarId));
    }
}
