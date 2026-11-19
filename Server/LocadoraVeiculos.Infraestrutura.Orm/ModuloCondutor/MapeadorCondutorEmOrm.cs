using LocadoraDeVeiculos.Dominio.ModuloCondutor;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LocadoraDeVeiculos.Infraestrutura.Orm.ModuloCondutor;

public class MapeadorCondutorEmOrm : IEntityTypeConfiguration<Condutor>
{
    public void Configure(EntityTypeBuilder<Condutor> builder)
    {
        builder.HasKey(c => c.Id);

        builder.Property(c => c.Nome).HasColumnType("nvarchar(150)").IsRequired();
        builder.Property(c => c.Email).HasColumnType("varchar(150)").IsRequired();
        builder.Property(c => c.Telefone).HasColumnType("varchar(15)").IsRequired();
        builder.Property(c => c.Cpf).HasColumnType("varchar(14)").IsRequired();
        builder.Property(c => c.Cnh).HasColumnType("varchar(11)").IsRequired();
        builder.Property(c => c.ValidadeCnh).HasColumnType("date").IsRequired();

        builder.HasOne(c => c.Cliente)
               .WithMany()
               .HasForeignKey(c => c.ClienteId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(c => c.Empresa)
               .WithMany()
               .HasForeignKey(c => c.EmpresaId)
               .OnDelete(DeleteBehavior.Restrict);

        // A mesma pessoa não se repete como condutor do mesmo cliente
        // (mas pode ser condutora de clientes diferentes).
        builder.HasIndex(c => new { c.EmpresaId, c.ClienteId, c.Cpf })
               .IsUnique()
               .HasFilter("[Excluido] = 0");
    }
}
