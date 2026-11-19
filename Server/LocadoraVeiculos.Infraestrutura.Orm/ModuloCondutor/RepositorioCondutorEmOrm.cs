using LocadoraDeVeiculos.Dominio.ModuloCondutor;
using LocadoraDeVeiculos.Infraestrutura.Orm.Compartilhado;
using Microsoft.EntityFrameworkCore;

namespace LocadoraDeVeiculos.Infraestrutura.Orm.ModuloCondutor;

public class RepositorioCondutorEmOrm : RepositorioBaseEmOrm<Condutor>
{
    private readonly AppDbContext dbContext;

    public RepositorioCondutorEmOrm(AppDbContext dbContext) : base(dbContext)
    {
        this.dbContext = dbContext;
    }

    public override async Task<Condutor?> SelecionarPorIdAsync(Guid idRegistro)
    {
        return await dbContext.Condutores
            .Include(c => c.Cliente)
            .FirstOrDefaultAsync(c => c.Id == idRegistro);
    }

    public async Task<List<Condutor>> SelecionarTodosAsync(Guid? clienteId)
    {
        IQueryable<Condutor> consulta = dbContext.Condutores.Include(c => c.Cliente);

        if (clienteId is not null)
            consulta = consulta.Where(c => c.ClienteId == clienteId);

        return await consulta.OrderBy(c => c.Nome).ToListAsync();
    }

    public override async Task<List<Condutor>> SelecionarTodosAsync()
    {
        return await SelecionarTodosAsync(null);
    }

    public async Task<bool> ExisteCpfNoClienteAsync(Guid clienteId, string cpf, Guid? ignorarId = null)
    {
        return await dbContext.Condutores
            .AnyAsync(c => c.ClienteId == clienteId && c.Cpf == cpf && (ignorarId == null || c.Id != ignorarId));
    }

    public async Task<bool> ExisteParaClienteAsync(Guid clienteId)
    {
        return await dbContext.Condutores.AnyAsync(c => c.ClienteId == clienteId);
    }
}
