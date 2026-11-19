using LocadoraDeVeiculos.Dominio.ModuloPlanoCobranca;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LocadoraDeVeiculos.Infraestrutura.Orm.ModuloPlanoCobranca;

public class MapeadorPlanoCobrancaEmOrm : IEntityTypeConfiguration<PlanoCobranca>
{
    public void Configure(EntityTypeBuilder<PlanoCobranca> builder)
    {
        builder.HasKey(p => p.Id);

        builder.Property(p => p.DiarioPrecoDiaria).HasColumnType("decimal(18,2)").IsRequired();
        builder.Property(p => p.DiarioPrecoPorKm).HasColumnType("decimal(18,2)").IsRequired();
        builder.Property(p => p.ControladoKmDisponiveis).IsRequired();
        builder.Property(p => p.ControladoPrecoDiaria).HasColumnType("decimal(18,2)").IsRequired();
        builder.Property(p => p.ControladoPrecoPorKmExtrapolado).HasColumnType("decimal(18,2)").IsRequired();
        builder.Property(p => p.LivrePrecoDiaria).HasColumnType("decimal(18,2)").IsRequired();

        builder.HasOne(p => p.GrupoVeiculos)
               .WithMany()
               .HasForeignKey(p => p.GrupoVeiculosId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(p => p.Empresa)
               .WithMany()
               .HasForeignKey(p => p.EmpresaId)
               .OnDelete(DeleteBehavior.Restrict);

        // Um plano por grupo. O filtro permite criar outro depois que o anterior foi excluído (exclusão lógica).
        builder.HasIndex(p => new { p.EmpresaId, p.GrupoVeiculosId })
               .IsUnique()
               .HasFilter("[Excluido] = 0");
    }
}
