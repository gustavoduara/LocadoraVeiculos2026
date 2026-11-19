using LocadoraDeVeiculos.Dominio.ModuloConfiguracao;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LocadoraDeVeiculos.Infraestrutura.Orm.ModuloConfiguracao;

public class MapeadorConfiguracaoEmOrm : IEntityTypeConfiguration<Configuracao>
{
    public void Configure(EntityTypeBuilder<Configuracao> builder)
    {
        builder.HasKey(c => c.Id);

        builder.Property(c => c.PrecoGasolina).HasColumnType("decimal(18,2)").IsRequired();
        builder.Property(c => c.PrecoGas).HasColumnType("decimal(18,2)").IsRequired();
        builder.Property(c => c.PrecoDiesel).HasColumnType("decimal(18,2)").IsRequired();
        builder.Property(c => c.PrecoAlcool).HasColumnType("decimal(18,2)").IsRequired();

        builder.HasOne(c => c.Empresa)
               .WithMany()
               .HasForeignKey(c => c.EmpresaId)
               .OnDelete(DeleteBehavior.Restrict);

        // Uma configuração por empresa.
        builder.HasIndex(c => c.EmpresaId).IsUnique();
    }
}
