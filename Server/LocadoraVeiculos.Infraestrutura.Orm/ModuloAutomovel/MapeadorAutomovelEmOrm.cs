using LocadoraDeVeiculos.Dominio.ModuloAutomovel;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LocadoraDeVeiculos.Infraestrutura.Orm.ModuloAutomovel;

public class MapeadorAutomovelEmOrm : IEntityTypeConfiguration<Automovel>
{
    public void Configure(EntityTypeBuilder<Automovel> builder)
    {
        builder.HasKey(a => a.Id);

        builder.Property(a => a.Placa).HasColumnType("varchar(8)").IsRequired();
        builder.Property(a => a.Marca).HasColumnType("nvarchar(50)").IsRequired();
        builder.Property(a => a.Modelo).HasColumnType("nvarchar(50)").IsRequired();
        builder.Property(a => a.Cor).HasColumnType("nvarchar(30)").IsRequired();
        builder.Property(a => a.Ano).IsRequired();
        builder.Property(a => a.TipoCombustivel).IsRequired();
        builder.Property(a => a.CapacidadeCombustivelLitros).IsRequired();
        builder.Property(a => a.SeguroDiaria).HasColumnType("decimal(18,2)").IsRequired();

        builder.Property(a => a.Foto).HasColumnType("varbinary(max)");
        builder.Property(a => a.FotoTipoConteudo).HasColumnType("varchar(50)");
        builder.Property(a => a.FotoNomeArquivo).HasColumnType("nvarchar(255)");

        builder.Ignore(a => a.TemFoto);

        builder.HasOne(a => a.GrupoVeiculos)
               .WithMany()
               .HasForeignKey(a => a.GrupoVeiculosId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(a => a.Empresa)
               .WithMany()
               .HasForeignKey(a => a.EmpresaId)
               .OnDelete(DeleteBehavior.Restrict);

        // Placa única por empresa (veículos excluídos logicamente não contam).
        builder.HasIndex(a => new { a.EmpresaId, a.Placa })
               .IsUnique()
               .HasFilter("[Excluido] = 0");
    }
}
