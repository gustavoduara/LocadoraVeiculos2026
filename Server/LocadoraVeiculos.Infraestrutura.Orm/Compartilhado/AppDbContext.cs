using LocadoraDeVeiculos.Dominio.ModuloAutenticacao;
using LocadoraDeVeiculos.Dominio.ModuloAluguel;
using LocadoraDeVeiculos.Dominio.ModuloAutomovel;
using LocadoraDeVeiculos.Dominio.ModuloCliente;
using LocadoraDeVeiculos.Dominio.ModuloCondutor;
using LocadoraDeVeiculos.Dominio.ModuloCupom;
using LocadoraDeVeiculos.Dominio.ModuloParceiro;
using LocadoraDeVeiculos.Dominio.ModuloPerfilCliente;
using LocadoraDeVeiculos.Dominio.ModuloReserva;
using LocadoraDeVeiculos.Dominio.ModuloConfiguracao;
using LocadoraDeVeiculos.Dominio.ModuloFuncionario;
using LocadoraDeVeiculos.Dominio.ModuloGrupoVeiculos;
using LocadoraDeVeiculos.Dominio.ModuloPlanoCobranca;
using LocadoraDeVeiculos.Dominio.ModuloTaxaServico;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace LocadoraDeVeiculos.Infraestrutura.Orm.Compartilhado;

public class AppDbContext(
    DbContextOptions options,
    ITenantProvider? tenantProvider = null
) : IdentityDbContext<Usuario, Cargo, Guid>(options)
{
    public DbSet<RefreshToken> RefreshTokens { get; set; }
    public DbSet<Funcionario> Funcionarios { get; set; }
    public DbSet<GrupoVeiculos> GruposVeiculos { get; set; }
    public DbSet<Configuracao> Configuracoes { get; set; }
    public DbSet<PlanoCobranca> PlanosCobranca { get; set; }
    public DbSet<Automovel> Automoveis { get; set; }
    public DbSet<TaxaServico> TaxasServicos { get; set; }
    public DbSet<Cliente> Clientes { get; set; }
    public DbSet<Condutor> Condutores { get; set; }
    public DbSet<Aluguel> Alugueis { get; set; }
    public DbSet<Parceiro> Parceiros { get; set; }
    public DbSet<Cupom> Cupons { get; set; }
    public DbSet<Reserva> Reservas { get; set; }
    public DbSet<PerfilCliente> PerfisClientes { get; set; }


    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        if (tenantProvider is not null)
        {
            // Query Filters
            modelBuilder.Entity<Aluguel>()
                        .HasQueryFilter(f => f.EmpresaId == tenantProvider.EmpresaId.GetValueOrDefault() && !f.Excluido);

            modelBuilder.Entity<Reserva>()
                        .HasQueryFilter(f => f.EmpresaId == tenantProvider.EmpresaId.GetValueOrDefault() && !f.Excluido);

            modelBuilder.Entity<Parceiro>()
                        .HasQueryFilter(f => f.EmpresaId == tenantProvider.EmpresaId.GetValueOrDefault() && !f.Excluido);

            modelBuilder.Entity<Cupom>()
                        .HasQueryFilter(f => f.EmpresaId == tenantProvider.EmpresaId.GetValueOrDefault() && !f.Excluido);

            modelBuilder.Entity<Condutor>()
                        .HasQueryFilter(f => f.EmpresaId == tenantProvider.EmpresaId.GetValueOrDefault() && !f.Excluido);

            modelBuilder.Entity<Cliente>()
                        .HasQueryFilter(f => f.EmpresaId == tenantProvider.EmpresaId.GetValueOrDefault() && !f.Excluido);

            modelBuilder.Entity<TaxaServico>()
                        .HasQueryFilter(f => f.EmpresaId == tenantProvider.EmpresaId.GetValueOrDefault() && !f.Excluido);

            modelBuilder.Entity<Automovel>()
                        .HasQueryFilter(f => f.EmpresaId == tenantProvider.EmpresaId.GetValueOrDefault() && !f.Excluido);

            modelBuilder.Entity<PlanoCobranca>()
                        .HasQueryFilter(f => f.EmpresaId == tenantProvider.EmpresaId.GetValueOrDefault() && !f.Excluido);

            modelBuilder.Entity<Configuracao>()
                        .HasQueryFilter(f => f.EmpresaId == tenantProvider.EmpresaId.GetValueOrDefault() && !f.Excluido);

            modelBuilder.Entity<Funcionario>()
                        .HasQueryFilter(f => f.EmpresaId == tenantProvider.EmpresaId.GetValueOrDefault() && !f.Excluido);

            modelBuilder.Entity<GrupoVeiculos>()
                        .HasQueryFilter(f => f.EmpresaId == tenantProvider.EmpresaId.GetValueOrDefault() && !f.Excluido);
        }

        var assembly = typeof(AppDbContext).Assembly;

        modelBuilder.ApplyConfigurationsFromAssembly(assembly);

        base.OnModelCreating(modelBuilder);
    }
}