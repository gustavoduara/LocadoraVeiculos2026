using LocadoraDeVeiculos.Dominio.ModuloTaxaServico;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LocadoraDeVeiculos.Infraestrutura.Orm.ModuloTaxaServico;

public class MapeadorTaxaServicoEmOrm : IEntityTypeConfiguration<TaxaServico>
{
    public void Configure(EntityTypeBuilder<TaxaServico> builder)
    {
        builder.HasKey(t => t.Id);

        builder.Property(t => t.Nome).HasColumnType("nvarchar(100)").IsRequired();
        builder.Property(t => t.Preco).HasColumnType("decimal(18,2)").IsRequired();
        builder.Property(t => t.TipoCobranca).IsRequired();

        builder.HasOne(t => t.Empresa)
               .WithMany()
               .HasForeignKey(t => t.EmpresaId)
               .OnDelete(DeleteBehavior.Restrict);

        // Nome único por empresa (itens excluídos logicamente não contam).
        builder.HasIndex(t => new { t.EmpresaId, t.Nome })
               .IsUnique()
               .HasFilter("[Excluido] = 0");
    }
}
