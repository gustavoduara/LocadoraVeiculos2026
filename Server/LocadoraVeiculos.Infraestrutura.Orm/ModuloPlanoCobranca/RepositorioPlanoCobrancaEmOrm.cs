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
            .Include(p => p.Automovel)
            .FirstOrDefaultAsync(p => p.Id == idRegistro);
    }

    public override async Task<List<PlanoCobranca>> SelecionarTodosAsync()
    {
        return await dbContext.PlanosCobranca
            .Include(p => p.Automovel)
            .OrderBy(p => p.Automovel!.Marca)
            .ThenBy(p => p.Automovel!.Modelo)
            .ThenBy(p => p.Automovel!.Placa)
            .ToListAsync();
    }

    public async Task<PlanoCobranca?> SelecionarPorAutomovelAsync(Guid automovelId)
    {
        return await dbContext.PlanosCobranca
            .FirstOrDefaultAsync(p => p.AutomovelId == automovelId);
    }

    public async Task<bool> ExisteParaAutomovelAsync(Guid automovelId)
    {
        return await dbContext.PlanosCobranca
            .AnyAsync(p => p.AutomovelId == automovelId);
    }
}
