using LocadoraDeVeiculos.Dominio.ModuloParceiro;
using LocadoraDeVeiculos.Infraestrutura.Orm.Compartilhado;
using Microsoft.EntityFrameworkCore;

namespace LocadoraDeVeiculos.Infraestrutura.Orm.ModuloParceiro;

public class RepositorioParceiroEmOrm : RepositorioBaseEmOrm<Parceiro>
{
    private readonly AppDbContext dbContext;

    public RepositorioParceiroEmOrm(AppDbContext dbContext) : base(dbContext)
    {
        this.dbContext = dbContext;
    }

    public override async Task<List<Parceiro>> SelecionarTodosAsync()
    {
        return await dbContext.Parceiros.OrderBy(p => p.Nome).ToListAsync();
    }

    /// <summary>Usa a collation do banco (sem diferenciar maiúsculas/minúsculas), igual ao índice único.</summary>
    public async Task<bool> ExisteNomeAsync(string nome, Guid? ignorarId = null)
    {
        return await dbContext.Parceiros
            .AnyAsync(p => p.Nome == nome && (ignorarId == null || p.Id != ignorarId));
    }
}
