using LocadoraDeVeiculos.Dominio.ModuloAutomovel;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LocadoraDeVeiculos.Infraestrutura.Orm.ModuloAutomovel;

public class MapeadorAutomovelFotoEmOrm : IEntityTypeConfiguration<AutomovelFoto>
{
    public void Configure(EntityTypeBuilder<AutomovelFoto> builder)
    {
        builder.HasKey(f => f.Id);

        builder.Property(f => f.Conteudo).HasColumnType("varbinary(max)").IsRequired();
        builder.Property(f => f.TipoConteudo).HasColumnType("varchar(50)").IsRequired();
        builder.Property(f => f.NomeArquivo).HasColumnType("nvarchar(255)").IsRequired();
        builder.Property(f => f.Ordem).IsRequired();

        builder.HasOne(f => f.Automovel)
               .WithMany(a => a.FotosExtras)
               .HasForeignKey(f => f.AutomovelId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(f => new { f.AutomovelId, f.Ordem });
    }
}
