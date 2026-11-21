using LocadoraDeVeiculos.Dominio.Compartilhado;
using LocadoraDeVeiculos.Dominio.ModuloAluguel;
using LocadoraDeVeiculos.Dominio.ModuloCliente;
using LocadoraDeVeiculos.Dominio.ModuloCupom;
using LocadoraDeVeiculos.Dominio.ModuloTaxaServico;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LocadoraDeVeiculos.Infraestrutura.Orm.ModuloAluguel;

public class MapeadorAluguelEmOrm : IEntityTypeConfiguration<Aluguel>
{
    public void Configure(EntityTypeBuilder<Aluguel> builder)
    {
        builder.HasKey(a => a.Id);

        builder.Property(a => a.NomeCondutor).HasColumnType("nvarchar(150)").IsRequired();
        builder.Property(a => a.NomeCliente).HasColumnType("nvarchar(150)").IsRequired();
        builder.Property(a => a.DescricaoVeiculo).HasColumnType("nvarchar(150)").IsRequired();
        builder.Property(a => a.NomeGrupo).HasColumnType("nvarchar(100)").IsRequired();

        builder.Property(a => a.DataSaida).HasColumnType("date").IsRequired();
        builder.Property(a => a.DataRetornoPrevista).HasColumnType("date").IsRequired();
        builder.Property(a => a.KmInicial).IsRequired();

        builder.Property(a => a.TipoPlano).IsRequired();
        builder.Property(a => a.PlanoPrecoDiaria).HasColumnType("decimal(18,2)").IsRequired();
        builder.Property(a => a.PlanoPrecoPorKm).HasColumnType("decimal(18,2)").IsRequired();
        builder.Property(a => a.PlanoKmDisponiveisPorDia).IsRequired();

        builder.Property(a => a.TipoSeguro).IsRequired();
        builder.Property(a => a.SeguroPrecoDiaria).HasColumnType("decimal(18,2)").IsRequired();
        builder.Property(a => a.ValorCaucao).HasColumnType("decimal(18,2)").IsRequired();

        builder.Property(a => a.CupomNome).HasColumnType("nvarchar(50)");
        builder.Property(a => a.CupomValor).HasColumnType("decimal(18,2)").IsRequired();
        builder.Property(a => a.CupomTipo).HasConversion<int>().HasDefaultValue(TipoDesconto.Valor).IsRequired();

        builder.Property(a => a.Status).IsRequired();
        builder.Property(a => a.DataDevolucao).HasColumnType("date");
        builder.Property(a => a.PrecoCombustivelPorLitro).HasColumnType("decimal(18,2)");

        // Valores derivados (calculados a partir dos dados acima) não são colunas.
        builder.Ignore(a => a.EstaEmAberto);
        builder.Ignore(a => a.Dias);
        builder.Ignore(a => a.ValorDiarias);
        builder.Ignore(a => a.ValorTaxas);
        builder.Ignore(a => a.ValorSeguro);

        builder.HasOne(a => a.Condutor).WithMany().HasForeignKey(a => a.CondutorId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Cliente>().WithMany().HasForeignKey(a => a.ClienteId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(a => a.Automovel).WithMany().HasForeignKey(a => a.AutomovelId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(a => a.PlanoCobranca).WithMany().HasForeignKey(a => a.PlanoCobrancaId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Cupom>().WithMany().HasForeignKey(a => a.CupomId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(a => a.Funcionario).WithMany().HasForeignKey(a => a.FuncionarioId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(a => a.Empresa).WithMany().HasForeignKey(a => a.EmpresaId).OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(a => a.Taxas)
               .WithOne()
               .HasForeignKey(t => t.AluguelId)
               .OnDelete(DeleteBehavior.Cascade);

        // Consultas de disponibilidade do veículo e de "aluguéis em aberto".
        builder.HasIndex(a => new { a.EmpresaId, a.AutomovelId, a.Status });
    }
}

public class MapeadorAluguelTaxaEmOrm : IEntityTypeConfiguration<AluguelTaxa>
{
    public void Configure(EntityTypeBuilder<AluguelTaxa> builder)
    {
        builder.HasKey(t => t.Id);

        builder.Property(t => t.Nome).HasColumnType("nvarchar(100)").IsRequired();
        builder.Property(t => t.PrecoUnitario).HasColumnType("decimal(18,2)").IsRequired();
        builder.Property(t => t.TipoCobranca).IsRequired();

        builder.HasOne<TaxaServico>()
               .WithMany()
               .HasForeignKey(t => t.TaxaServicoId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(t => t.TaxaServicoId);
    }
}
