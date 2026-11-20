using LocadoraDeVeiculos.Dominio.ModuloParceiro;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LocadoraDeVeiculos.Infraestrutura.Orm.ModuloParceiro;

public class MapeadorParceiroEmOrm : IEntityTypeConfiguration<Parceiro>
{
    public void Configure(EntityTypeBuilder<Parceiro> builder)
    {
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Nome).HasColumnType("nvarchar(100)").IsRequired();

        builder.HasOne(p => p.Empresa)
               .WithMany()
               .HasForeignKey(p => p.EmpresaId)
               .OnDelete(DeleteBehavior.Restrict);

        // Nome único por empresa (parceiros excluídos logicamente não contam).
        builder.HasIndex(p => new { p.EmpresaId, p.Nome })
               .IsUnique()
               .HasFilter("[Excluido] = 0");
    }
}
