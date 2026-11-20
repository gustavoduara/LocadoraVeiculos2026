using LocadoraDeVeiculos.Dominio.ModuloCupom;
using LocadoraDeVeiculos.Infraestrutura.Orm.Compartilhado;
using Microsoft.EntityFrameworkCore;

namespace LocadoraDeVeiculos.Infraestrutura.Orm.ModuloCupom;

public class RepositorioCupomEmOrm : RepositorioBaseEmOrm<Cupom>
{
    private readonly AppDbContext dbContext;

    public RepositorioCupomEmOrm(AppDbContext dbContext) : base(dbContext)
    {
        this.dbContext = dbContext;
    }

    public override async Task<Cupom?> SelecionarPorIdAsync(Guid idRegistro)
    {
        return await dbContext.Cupons
            .Include(c => c.Parceiro)
            .FirstOrDefaultAsync(c => c.Id == idRegistro);
    }

    public override async Task<List<Cupom>> SelecionarTodosAsync()
    {
        return await dbContext.Cupons
            .Include(c => c.Parceiro)
            .OrderBy(c => c.Nome)
            .ToListAsync();
    }

    public async Task<List<Cupom>> SelecionarPorIdsAsync(IReadOnlyCollection<Guid> ids)
    {
        return await dbContext.Cupons
            .Include(c => c.Parceiro)
            .Where(c => ids.Contains(c.Id))
            .ToListAsync();
    }

    public async Task<bool> ExisteNomeAsync(string nome, Guid? ignorarId = null)
    {
        return await dbContext.Cupons
            .AnyAsync(c => c.Nome == nome && (ignorarId == null || c.Id != ignorarId));
    }

    public async Task<bool> ExisteParaParceiroAsync(Guid parceiroId)
    {
        return await dbContext.Cupons.AnyAsync(c => c.ParceiroId == parceiroId);
    }
}
