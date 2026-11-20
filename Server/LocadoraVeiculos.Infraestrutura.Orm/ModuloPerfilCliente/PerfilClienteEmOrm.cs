using LocadoraDeVeiculos.Dominio.ModuloAutenticacao;
using LocadoraDeVeiculos.Dominio.ModuloPerfilCliente;
using LocadoraDeVeiculos.Infraestrutura.Orm.Compartilhado;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LocadoraDeVeiculos.Infraestrutura.Orm.ModuloPerfilCliente;

public class MapeadorPerfilClienteEmOrm : IEntityTypeConfiguration<PerfilCliente>
{
    public void Configure(EntityTypeBuilder<PerfilCliente> builder)
    {
        builder.HasKey(p => p.UsuarioId);

        builder.Property(p => p.Cpf).HasColumnType("nvarchar(14)").IsRequired();
        builder.Property(p => p.Rg).HasColumnType("nvarchar(20)").IsRequired();
        builder.Property(p => p.Cnh).HasColumnType("nvarchar(11)").IsRequired();
        builder.Property(p => p.ValidadeCnh).HasColumnType("date").IsRequired();
        builder.Property(p => p.Telefone).HasColumnType("nvarchar(20)").IsRequired();
        builder.Property(p => p.Estado).HasColumnType("nvarchar(2)").IsRequired();
        builder.Property(p => p.Cidade).HasColumnType("nvarchar(100)").IsRequired();
        builder.Property(p => p.Bairro).HasColumnType("nvarchar(100)").IsRequired();
        builder.Property(p => p.Rua).HasColumnType("nvarchar(150)").IsRequired();
        builder.Property(p => p.Numero).HasColumnType("nvarchar(10)").IsRequired();

        builder.HasOne<Usuario>()
               .WithMany()
               .HasForeignKey(p => p.UsuarioId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}

/// <summary>Perfil de cliente não pertence a uma locadora: não há filtro por empresa, só pelo dono da conta.</summary>
public class RepositorioPerfilClienteEmOrm(AppDbContext dbContext)
{
    public async Task<PerfilCliente?> SelecionarAsync(Guid usuarioId) =>
        await dbContext.PerfisClientes.FirstOrDefaultAsync(p => p.UsuarioId == usuarioId);

    public async Task CadastrarAsync(PerfilCliente perfil) => await dbContext.PerfisClientes.AddAsync(perfil);
}
