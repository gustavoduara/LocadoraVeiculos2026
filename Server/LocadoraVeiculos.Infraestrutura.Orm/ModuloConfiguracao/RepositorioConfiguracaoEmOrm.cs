using LocadoraDeVeiculos.Dominio.ModuloConfiguracao;
using LocadoraDeVeiculos.Infraestrutura.Orm.Compartilhado;
using Microsoft.EntityFrameworkCore;

namespace LocadoraDeVeiculos.Infraestrutura.Orm.ModuloConfiguracao;

public class RepositorioConfiguracaoEmOrm : RepositorioBaseEmOrm<Configuracao>
{
    private readonly AppDbContext dbContext;

    public RepositorioConfiguracaoEmOrm(AppDbContext dbContext) : base(dbContext)
    {
        this.dbContext = dbContext;
    }

    /// <summary>Configuração da empresa logada (o filtro global já restringe ao tenant).</summary>
    public async Task<Configuracao?> SelecionarDaEmpresaAsync()
    {
        return await dbContext.Configuracoes.FirstOrDefaultAsync();
    }
}
