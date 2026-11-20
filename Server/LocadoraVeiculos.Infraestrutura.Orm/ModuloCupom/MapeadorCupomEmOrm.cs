using LocadoraDeVeiculos.Dominio.ModuloCupom;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LocadoraDeVeiculos.Infraestrutura.Orm.ModuloCupom;

public class MapeadorCupomEmOrm : IEntityTypeConfiguration<Cupom>
{
    public void Configure(EntityTypeBuilder<Cupom> builder)
    {
        builder.HasKey(c => c.Id);

        builder.Property(c => c.Nome).HasColumnType("nvarchar(50)").IsRequired();
        builder.Property(c => c.Valor).HasColumnType("decimal(18,2)").IsRequired();
        builder.Property(c => c.DataValidade).HasColumnType("date").IsRequired();

        builder.HasOne(c => c.Parceiro)
               .WithMany()
               .HasForeignKey(c => c.ParceiroId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(c => c.Empresa)
               .WithMany()
               .HasForeignKey(c => c.EmpresaId)
               .OnDelete(DeleteBehavior.Restrict);

        // Código do cupom único por empresa (cupons excluídos logicamente não contam).
        builder.HasIndex(c => new { c.EmpresaId, c.Nome })
               .IsUnique()
               .HasFilter("[Excluido] = 0");
    }
}
