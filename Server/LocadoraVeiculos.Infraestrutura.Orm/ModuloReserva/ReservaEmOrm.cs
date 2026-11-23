using LocadoraDeVeiculos.Dominio.Compartilhado;
using LocadoraDeVeiculos.Dominio.ModuloAluguel;
using LocadoraDeVeiculos.Dominio.ModuloAutomovel;
using LocadoraDeVeiculos.Dominio.ModuloConfiguracao;
using LocadoraDeVeiculos.Dominio.ModuloCupom;
using LocadoraDeVeiculos.Dominio.ModuloPlanoCobranca;
using LocadoraDeVeiculos.Dominio.ModuloReserva;
using LocadoraDeVeiculos.Dominio.ModuloTaxaServico;
using LocadoraDeVeiculos.Infraestrutura.Orm.Compartilhado;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LocadoraDeVeiculos.Infraestrutura.Orm.ModuloReserva;

public class MapeadorReservaEmOrm : IEntityTypeConfiguration<Reserva>
{
    public void Configure(EntityTypeBuilder<Reserva> builder)
    {
        builder.HasKey(r => r.Id);

        builder.Property(r => r.NomeLocadora).HasColumnType("nvarchar(150)").IsRequired();
        builder.Property(r => r.DescricaoVeiculo).HasColumnType("nvarchar(150)").IsRequired();
        builder.Property(r => r.NomeGrupo).HasColumnType("nvarchar(100)").IsRequired();

        builder.Property(r => r.Retirada).HasColumnType("date").IsRequired();
        builder.Property(r => r.Devolucao).HasColumnType("date").IsRequired();
        builder.Property(r => r.TipoPlano).IsRequired();
        builder.Property(r => r.TipoSeguro).IsRequired();

        builder.Property(r => r.ValorDiarias).HasColumnType("decimal(18,2)").IsRequired();
        builder.Property(r => r.ValorSeguro).HasColumnType("decimal(18,2)").IsRequired();
        builder.Property(r => r.ValorCaucao).HasColumnType("decimal(18,2)").IsRequired();
        builder.Property(r => r.ValorTotalPrevisto).HasColumnType("decimal(18,2)").IsRequired();
        builder.Property(r => r.ValorTaxas).HasColumnType("decimal(18,2)").IsRequired();
        builder.Property(r => r.ValorDesconto).HasColumnType("decimal(18,2)").IsRequired();

        builder.Property(r => r.CupomNome).HasColumnType("nvarchar(50)");
        builder.Property(r => r.CupomValor).HasColumnType("decimal(18,2)").IsRequired();
        builder.Property(r => r.CupomTipo).HasConversion<int>().HasDefaultValue(TipoDesconto.Valor).IsRequired();

        builder.HasMany(r => r.Taxas)
               .WithOne()
               .HasForeignKey(t => t.ReservaId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.Property(r => r.ClienteNome).HasColumnType("nvarchar(150)").IsRequired();
        builder.Property(r => r.ClienteEmail).HasColumnType("nvarchar(256)").IsRequired();
        builder.Property(r => r.ClienteTelefone).HasColumnType("nvarchar(20)").IsRequired();
        builder.Property(r => r.ClienteCpf).HasColumnType("nvarchar(14)").IsRequired();
        builder.Property(r => r.ClienteRg).HasColumnType("nvarchar(20)").IsRequired();
        builder.Property(r => r.ClienteCnh).HasColumnType("nvarchar(11)").IsRequired();
        builder.Property(r => r.ClienteValidadeCnh).HasColumnType("date").IsRequired();
        builder.Property(r => r.ClienteEstado).HasColumnType("nvarchar(2)").IsRequired();
        builder.Property(r => r.ClienteCidade).HasColumnType("nvarchar(100)").IsRequired();
        builder.Property(r => r.ClienteBairro).HasColumnType("nvarchar(100)").IsRequired();
        builder.Property(r => r.ClienteRua).HasColumnType("nvarchar(150)").IsRequired();
        builder.Property(r => r.ClienteNumero).HasColumnType("nvarchar(10)").IsRequired();

        builder.Property(r => r.Status).IsRequired();
        builder.Property(r => r.MotivoRecusa).HasColumnType("nvarchar(300)");

        builder.Ignore(r => r.EstaPendente);

        builder.HasOne(r => r.Automovel).WithMany().HasForeignKey(r => r.AutomovelId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Aluguel>().WithMany().HasForeignKey(r => r.AluguelId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(r => r.Empresa).WithMany().HasForeignKey(r => r.EmpresaId).OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(r => new { r.UsuarioClienteId, r.Status });
        builder.HasIndex(r => new { r.EmpresaId, r.Status });
    }
}

public class MapeadorReservaTaxaEmOrm : IEntityTypeConfiguration<ReservaTaxa>
{
    public void Configure(EntityTypeBuilder<ReservaTaxa> builder)
    {
        builder.HasKey(t => t.Id);

        builder.Property(t => t.Nome).HasColumnType("nvarchar(100)").IsRequired();
        builder.Property(t => t.PrecoUnitario).HasColumnType("decimal(18,2)").IsRequired();
        builder.Property(t => t.TipoCobranca).IsRequired();
    }
}

/// <summary>Tudo o que o pedido de reserva precisa saber de um carro, de qualquer locadora.</summary>
public record CarroReservavel(
    Guid AutomovelId,
    Guid EmpresaId,
    string Marca,
    string Modelo,
    int Ano,
    string NomeGrupo,
    decimal SeguroDiaria,
    PlanoCobranca? Plano,
    Configuracao? Configuracao
);

/// <summary>
/// Reservas vistas pelas duas pontas. O lado do cliente atravessa as locadoras (por isso ignora o filtro por empresa e
/// filtra pelo dono do pedido); o lado da locadora usa o filtro normal.
/// </summary>
public class RepositorioReservaEmOrm(AppDbContext dbContext) : RepositorioBaseEmOrm<Reserva>(dbContext)
{
    // ---------------- lado do cliente (cross-tenant) ----------------

    public async Task<CarroReservavel?> SelecionarCarroAsync(Guid automovelId)
    {
        var carro = await dbContext.Automoveis
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(a => a.Id == automovelId && !a.Excluido && !a.GrupoVeiculos!.Excluido)
            .Select(a => new { a.Id, a.EmpresaId, a.Marca, a.Modelo, a.Ano, Grupo = a.GrupoVeiculos!.Nome, a.GrupoVeiculosId, a.SeguroDiaria })
            .FirstOrDefaultAsync();

        if (carro is null)
            return null;

        PlanoCobranca? plano = await dbContext.PlanosCobranca
            .IgnoreQueryFilters()
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.GrupoVeiculosId == carro.GrupoVeiculosId && !p.Excluido);

        Configuracao? configuracao = await dbContext.Configuracoes
            .IgnoreQueryFilters()
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.EmpresaId == carro.EmpresaId && !c.Excluido);

        return new CarroReservavel(carro.Id, carro.EmpresaId, carro.Marca, carro.Modelo, carro.Ano, carro.Grupo, carro.SeguroDiaria, plano, configuracao);
    }

    public async Task<bool> CarroOcupadoAsync(Guid automovelId, DateOnly retirada, DateOnly devolucao, DateOnly hoje)
    {
        return await dbContext.Alugueis
            .IgnoreQueryFilters()
            .AnyAsync(a =>
                !a.Excluido &&
                a.AutomovelId == automovelId &&
                a.Status == StatusAluguel.EmAberto &&
                a.DataSaida <= devolucao &&
                (a.DataRetornoPrevista >= hoje ? a.DataRetornoPrevista : hoje) >= retirada);
    }

    /// <summary>Taxas e serviços do catálogo de uma locadora (o cliente escolhe entre elas ao reservar).</summary>
    public async Task<List<TaxaServico>> SelecionarTaxasDaLocadoraAsync(Guid empresaId)
    {
        return await dbContext.TaxasServicos
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(t => t.EmpresaId == empresaId && !t.Excluido)
            .OrderBy(t => t.Nome)
            .ToListAsync();
    }

    /// <summary>O cupom que o cliente digitou, procurado pelo nome (sem diferenciar maiúsculas) na locadora do carro.</summary>
    public async Task<Cupom?> SelecionarCupomPorNomeAsync(Guid empresaId, string nome)
    {
        string procurado = nome.Trim().ToUpper();

        return await dbContext.Cupons
            .IgnoreQueryFilters()
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.EmpresaId == empresaId && !c.Excluido && c.Nome.ToUpper() == procurado);
    }

    /// <summary>
    /// Cada cliente usa um cupom uma vez por locadora: vale se ele já tem um pedido pendente ou aprovado com o cupom,
    /// ou um aluguel (pelo mesmo CPF) que o usou.
    /// </summary>
    public async Task<bool> ClienteJaUsouCupomAsync(Guid usuarioClienteId, Guid empresaId, string? cpf, Guid cupomId)
    {
        if (await dbContext.Reservas.IgnoreQueryFilters().AnyAsync(r =>
                !r.Excluido && r.UsuarioClienteId == usuarioClienteId && r.CupomId == cupomId &&
                (r.Status == StatusReserva.Pendente || r.Status == StatusReserva.Aprovada)))
            return true;

        if (string.IsNullOrWhiteSpace(cpf))
            return false;

        Guid? clienteId = await dbContext.Clientes.IgnoreQueryFilters()
            .Where(c => c.EmpresaId == empresaId && !c.Excluido && c.Cpf == cpf)
            .Select(c => (Guid?)c.Id)
            .FirstOrDefaultAsync();

        return clienteId is not null &&
               await dbContext.Alugueis.IgnoreQueryFilters().AnyAsync(a => !a.Excluido && a.ClienteId == clienteId && a.CupomId == cupomId);
    }

    public override async Task<Reserva?> SelecionarPorIdAsync(Guid idRegistro)
    {
        return await dbContext.Reservas.Include(r => r.Taxas).FirstOrDefaultAsync(r => r.Id == idRegistro);
    }

    public async Task<List<Reserva>> SelecionarDoClienteAsync(Guid usuarioClienteId)
    {
        return await dbContext.Reservas
            .Include(r => r.Taxas)
            .IgnoreQueryFilters()
            .Where(r => !r.Excluido && r.UsuarioClienteId == usuarioClienteId)
            .OrderBy(r => r.Status)
            .ThenByDescending(r => r.CriadoEmUtc)
            .ToListAsync();
    }

    public async Task<Reserva?> SelecionarDoClientePorIdAsync(Guid id, Guid usuarioClienteId)
    {
        return await dbContext.Reservas
            .Include(r => r.Taxas)
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(r => r.Id == id && !r.Excluido && r.UsuarioClienteId == usuarioClienteId);
    }

    public async Task<int> ContarPendentesDoClienteAsync(Guid usuarioClienteId)
    {
        return await dbContext.Reservas
            .IgnoreQueryFilters()
            .CountAsync(r => !r.Excluido && r.UsuarioClienteId == usuarioClienteId && r.Status == StatusReserva.Pendente);
    }

    /// <summary>O mesmo cliente não pede duas vezes o mesmo carro em períodos que se sobrepõem.</summary>
    public async Task<bool> ExistePendenteSobrepostaAsync(Guid usuarioClienteId, Guid automovelId, DateOnly retirada, DateOnly devolucao)
    {
        return await dbContext.Reservas
            .IgnoreQueryFilters()
            .AnyAsync(r =>
                !r.Excluido &&
                r.UsuarioClienteId == usuarioClienteId &&
                r.AutomovelId == automovelId &&
                r.Status == StatusReserva.Pendente &&
                r.Retirada <= devolucao &&
                r.Devolucao >= retirada);
    }

    public async Task<Dictionary<Guid, string>> TelefonesDasLocadorasAsync(IReadOnlyCollection<Guid> empresaIds)
    {
        return await dbContext.Configuracoes
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(c => !c.Excluido && empresaIds.Contains(c.EmpresaId) && c.Telefone != null)
            .ToDictionaryAsync(c => c.EmpresaId, c => c.Telefone!);
    }

    // ---------------- lado da locadora (filtro normal por empresa) ----------------

    public async Task<List<Reserva>> SelecionarDaLocadoraAsync(StatusReserva? status)
    {
        IQueryable<Reserva> consulta = dbContext.Reservas.Include(r => r.Taxas);

        if (status is not null)
            consulta = consulta.Where(r => r.Status == status);

        // Pendentes primeiro (é o que a locadora precisa atender); dentro de cada grupo, as mais antigas antes.
        return await consulta.OrderBy(r => r.Status).ThenBy(r => r.CriadoEmUtc).ToListAsync();
    }

    public async Task<int> ContarPendentesDaLocadoraAsync()
    {
        return await dbContext.Reservas.CountAsync(r => r.Status == StatusReserva.Pendente);
    }
}
