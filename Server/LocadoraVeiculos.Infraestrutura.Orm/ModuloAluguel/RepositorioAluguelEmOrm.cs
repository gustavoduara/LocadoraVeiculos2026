using LocadoraDeVeiculos.Dominio.ModuloAluguel;
using LocadoraDeVeiculos.Infraestrutura.Orm.Compartilhado;
using Microsoft.EntityFrameworkCore;

namespace LocadoraDeVeiculos.Infraestrutura.Orm.ModuloAluguel;

public class RepositorioAluguelEmOrm : RepositorioBaseEmOrm<Aluguel>
{
    private readonly AppDbContext dbContext;

    public RepositorioAluguelEmOrm(AppDbContext dbContext) : base(dbContext)
    {
        this.dbContext = dbContext;
    }

    public override async Task<Aluguel?> SelecionarPorIdAsync(Guid idRegistro)
    {
        return await dbContext.Alugueis
            .Include(a => a.Taxas)
            .FirstOrDefaultAsync(a => a.Id == idRegistro);
    }

    public async Task<List<Aluguel>> SelecionarTodosAsync(StatusAluguel? status)
    {
        IQueryable<Aluguel> consulta = dbContext.Alugueis.Include(a => a.Taxas);

        if (status is not null)
            consulta = consulta.Where(a => a.Status == status);

        // Em aberto primeiro; dentro de cada grupo, os mais recentes antes.
        return await consulta
            .OrderBy(a => a.Status)
            .ThenByDescending(a => a.DataSaida)
            .ToListAsync();
    }

    public override async Task<List<Aluguel>> SelecionarTodosAsync()
    {
        return await SelecionarTodosAsync(null);
    }

    /// <summary>
    /// O veículo está indisponível se houver outro aluguel em aberto cujo período se sobreponha ao pedido.
    /// Um aluguel em aberto já vencido (veículo ainda não devolvido) continua ocupando o carro até hoje.
    /// </summary>
    public async Task<bool> ExisteConflitoDeDisponibilidadeAsync(
        Guid automovelId, DateOnly saida, DateOnly retorno, DateOnly hoje, Guid? ignorarId = null)
    {
        return await dbContext.Alugueis.AnyAsync(a =>
            a.AutomovelId == automovelId &&
            a.Status == StatusAluguel.EmAberto &&
            (ignorarId == null || a.Id != ignorarId) &&
            a.DataSaida <= retorno &&
            (a.DataRetornoPrevista >= hoje ? a.DataRetornoPrevista : hoje) >= saida);
    }

    public async Task<bool> ExisteEmAbertoParaAutomovelAsync(Guid automovelId) =>
        await dbContext.Alugueis.AnyAsync(a => a.AutomovelId == automovelId && a.Status == StatusAluguel.EmAberto);

    public async Task<bool> ExisteEmAbertoParaCondutorAsync(Guid condutorId) =>
        await dbContext.Alugueis.AnyAsync(a => a.CondutorId == condutorId && a.Status == StatusAluguel.EmAberto);

    public async Task<bool> ExisteEmAbertoParaClienteAsync(Guid clienteId) =>
        await dbContext.Alugueis.AnyAsync(a => a.ClienteId == clienteId && a.Status == StatusAluguel.EmAberto);

    public async Task<bool> ExisteEmAbertoParaFuncionarioAsync(Guid funcionarioId) =>
        await dbContext.Alugueis.AnyAsync(a => a.FuncionarioId == funcionarioId && a.Status == StatusAluguel.EmAberto);

    public async Task<bool> ExisteEmAbertoParaPlanoAsync(Guid planoCobrancaId) =>
        await dbContext.Alugueis.AnyAsync(a => a.PlanoCobrancaId == planoCobrancaId && a.Status == StatusAluguel.EmAberto);

    public async Task<bool> ExisteEmAbertoParaTaxaAsync(Guid taxaServicoId) =>
        await dbContext.Alugueis.AnyAsync(a =>
            a.Status == StatusAluguel.EmAberto && a.Taxas.Any(t => t.TaxaServicoId == taxaServicoId));
}
