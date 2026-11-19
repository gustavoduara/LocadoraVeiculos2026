using LocadoraDeVeiculos.Dominio.ModuloCliente;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LocadoraDeVeiculos.Infraestrutura.Orm.ModuloCliente;

public class MapeadorClienteEmOrm : IEntityTypeConfiguration<Cliente>
{
    public void Configure(EntityTypeBuilder<Cliente> builder)
    {
        builder.HasKey(c => c.Id);

        builder.Property(c => c.Tipo).IsRequired();

        builder.Property(c => c.Nome).HasColumnType("nvarchar(150)").IsRequired();
        builder.Property(c => c.Email).HasColumnType("varchar(150)").IsRequired();
        builder.Property(c => c.Telefone).HasColumnType("varchar(15)").IsRequired();

        builder.Property(c => c.Cpf).HasColumnType("varchar(14)");
        builder.Property(c => c.Rg).HasColumnType("varchar(20)");
        builder.Property(c => c.Cnh).HasColumnType("varchar(11)");
        builder.Property(c => c.Cnpj).HasColumnType("varchar(18)");

        builder.Property(c => c.Estado).HasColumnType("varchar(2)").IsRequired();
        builder.Property(c => c.Cidade).HasColumnType("nvarchar(100)").IsRequired();
        builder.Property(c => c.Bairro).HasColumnType("nvarchar(100)").IsRequired();
        builder.Property(c => c.Rua).HasColumnType("nvarchar(150)").IsRequired();
        builder.Property(c => c.Numero).HasColumnType("varchar(10)").IsRequired();

        builder.HasOne(c => c.PessoaJuridica)
               .WithMany()
               .HasForeignKey(c => c.PessoaJuridicaId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(c => c.Empresa)
               .WithMany()
               .HasForeignKey(c => c.EmpresaId)
               .OnDelete(DeleteBehavior.Restrict);

        // CPF e CNPJ únicos por empresa (registros excluídos logicamente não contam).
        builder.HasIndex(c => new { c.EmpresaId, c.Cpf })
               .IsUnique()
               .HasFilter("[Excluido] = 0 AND [Cpf] IS NOT NULL");

        builder.HasIndex(c => new { c.EmpresaId, c.Cnpj })
               .IsUnique()
               .HasFilter("[Excluido] = 0 AND [Cnpj] IS NOT NULL");
    }
}
