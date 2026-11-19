using LocadoraDeVeiculos.Dominio.ModuloPlanoCobranca;
using LocadoraDeVeiculos.Infraestrutura.Orm.Compartilhado;
using Microsoft.EntityFrameworkCore;

namespace LocadoraDeVeiculos.Infraestrutura.Orm.ModuloPlanoCobranca;

public class RepositorioPlanoCobrancaEmOrm : RepositorioBaseEmOrm<PlanoCobranca>
{
    private readonly AppDbContext dbContext;

    public RepositorioPlanoCobrancaEmOrm(AppDbContext dbContext) : base(dbContext)
    {
        this.dbContext = dbContext;
    }

    public override async Task<PlanoCobranca?> SelecionarPorIdAsync(Guid idRegistro)
    {
        return await dbContext.PlanosCobranca
            .Include(p => p.GrupoVeiculos)
            .FirstOrDefaultAsync(p => p.Id == idRegistro);
    }

    public override async Task<List<PlanoCobranca>> SelecionarTodosAsync()
    {
        return await dbContext.PlanosCobranca
            .Include(p => p.GrupoVeiculos)
            .OrderBy(p => p.GrupoVeiculos!.Nome)
            .ToListAsync();
    }

    public async Task<PlanoCobranca?> SelecionarPorGrupoAsync(Guid grupoVeiculosId)
    {
        return await dbContext.PlanosCobranca
            .FirstOrDefaultAsync(p => p.GrupoVeiculosId == grupoVeiculosId);
    }

    public async Task<bool> ExisteParaGrupoAsync(Guid grupoVeiculosId)
    {
        return await dbContext.PlanosCobranca
            .AnyAsync(p => p.GrupoVeiculosId == grupoVeiculosId);
    }
}
