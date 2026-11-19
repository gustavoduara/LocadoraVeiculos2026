using LocadoraDeVeiculos.Dominio.ModuloCliente;
using LocadoraDeVeiculos.Infraestrutura.Orm.Compartilhado;
using Microsoft.EntityFrameworkCore;

namespace LocadoraDeVeiculos.Infraestrutura.Orm.ModuloCliente;

public class RepositorioClienteEmOrm : RepositorioBaseEmOrm<Cliente>
{
    private readonly AppDbContext dbContext;

    public RepositorioClienteEmOrm(AppDbContext dbContext) : base(dbContext)
    {
        this.dbContext = dbContext;
    }

    public override async Task<Cliente?> SelecionarPorIdAsync(Guid idRegistro)
    {
        return await dbContext.Clientes
            .Include(c => c.PessoaJuridica)
            .FirstOrDefaultAsync(c => c.Id == idRegistro);
    }

    public async Task<List<Cliente>> SelecionarTodosAsync(TipoCliente? tipo)
    {
        IQueryable<Cliente> consulta = dbContext.Clientes.Include(c => c.PessoaJuridica);

        if (tipo is not null)
            consulta = consulta.Where(c => c.Tipo == tipo);

        return await consulta.OrderBy(c => c.Nome).ToListAsync();
    }

    public override async Task<List<Cliente>> SelecionarTodosAsync()
    {
        return await SelecionarTodosAsync(null);
    }

    public async Task<bool> ExisteCpfAsync(string cpf, Guid? ignorarId = null)
    {
        return await dbContext.Clientes
            .AnyAsync(c => c.Cpf == cpf && (ignorarId == null || c.Id != ignorarId));
    }

    public async Task<bool> ExisteCnpjAsync(string cnpj, Guid? ignorarId = null)
    {
        return await dbContext.Clientes
            .AnyAsync(c => c.Cnpj == cnpj && (ignorarId == null || c.Id != ignorarId));
    }

    public async Task<bool> ExistePessoaFisicaVinculadaAsync(Guid pessoaJuridicaId)
    {
        return await dbContext.Clientes
            .AnyAsync(c => c.PessoaJuridicaId == pessoaJuridicaId);
    }
}
