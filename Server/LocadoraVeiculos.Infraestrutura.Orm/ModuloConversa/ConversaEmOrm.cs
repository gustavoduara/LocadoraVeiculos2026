using LocadoraDeVeiculos.Dominio.ModuloConversa;
using LocadoraDeVeiculos.Infraestrutura.Orm.Compartilhado;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LocadoraDeVeiculos.Infraestrutura.Orm.ModuloConversa;

public class MapeadorConversaEmOrm : IEntityTypeConfiguration<Conversa>
{
    public void Configure(EntityTypeBuilder<Conversa> builder)
    {
        builder.HasKey(c => c.Id);

        builder.Property(c => c.ClienteNome).HasColumnType("nvarchar(150)").IsRequired();
        builder.Property(c => c.NomeLocadora).HasColumnType("nvarchar(150)").IsRequired();
        builder.Property(c => c.UltimaMensagemResumo).HasColumnType("nvarchar(200)").IsRequired();

        builder.HasMany(c => c.Mensagens)
               .WithOne()
               .HasForeignKey(m => m.ConversaId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(c => c.Empresa)
               .WithMany()
               .HasForeignKey(c => c.EmpresaId)
               .OnDelete(DeleteBehavior.Restrict);

        // Um cliente tem uma conversa com cada locadora.
        builder.HasIndex(c => new { c.EmpresaId, c.UsuarioClienteId })
               .IsUnique()
               .HasFilter("[Excluido] = 0");

        builder.HasIndex(c => new { c.UsuarioClienteId, c.UltimaMensagemEmUtc });
    }
}

public class MapeadorMensagemEmOrm : IEntityTypeConfiguration<Mensagem>
{
    public void Configure(EntityTypeBuilder<Mensagem> builder)
    {
        builder.HasKey(m => m.Id);

        builder.Property(m => m.AutorNome).HasColumnType("nvarchar(150)").IsRequired();
        builder.Property(m => m.Texto).HasColumnType("nvarchar(1000)").IsRequired();
        builder.Property(m => m.DescricaoVeiculo).HasColumnType("nvarchar(150)");

        builder.HasIndex(m => new { m.ConversaId, m.EnviadaEmUtc });
    }
}

/// <summary>
/// Conversas. O lado da locadora usa o filtro normal por empresa; o lado do cliente atravessa as locadoras
/// (ignora o filtro) e só enxerga o que é dele.
/// </summary>
public class RepositorioConversaEmOrm(AppDbContext dbContext)
{
    public async Task CadastrarAsync(Conversa conversa) => await dbContext.Conversas.AddAsync(conversa);

    public async Task CadastrarAsync(Mensagem mensagem) => await dbContext.Mensagens.AddAsync(mensagem);

    // ---------------- cliente ----------------

    public async Task<Conversa?> SelecionarDoClientePorLocadoraAsync(Guid usuarioClienteId, Guid empresaId) =>
        await dbContext.Conversas.IgnoreQueryFilters()
            .FirstOrDefaultAsync(c => !c.Excluido && c.UsuarioClienteId == usuarioClienteId && c.EmpresaId == empresaId);

    public async Task<Conversa?> SelecionarDoClientePorIdAsync(Guid id, Guid usuarioClienteId) =>
        await dbContext.Conversas.IgnoreQueryFilters()
            .FirstOrDefaultAsync(c => c.Id == id && !c.Excluido && c.UsuarioClienteId == usuarioClienteId);

    public async Task<List<Conversa>> ListarDoClienteAsync(Guid usuarioClienteId) =>
        await dbContext.Conversas.IgnoreQueryFilters().AsNoTracking()
            .Where(c => !c.Excluido && c.UsuarioClienteId == usuarioClienteId)
            .OrderByDescending(c => c.UltimaMensagemEmUtc)
            .ToListAsync();

    public async Task<int> TotalNaoLidasDoClienteAsync(Guid usuarioClienteId) =>
        await dbContext.Conversas.IgnoreQueryFilters()
            .Where(c => !c.Excluido && c.UsuarioClienteId == usuarioClienteId)
            .SumAsync(c => c.NaoLidasCliente);

    // ---------------- locadora (filtro normal por empresa) ----------------

    public async Task<Conversa?> SelecionarDaLocadoraPorIdAsync(Guid id) =>
        await dbContext.Conversas.FirstOrDefaultAsync(c => c.Id == id);

    public async Task<List<Conversa>> ListarDaLocadoraAsync() =>
        await dbContext.Conversas.AsNoTracking()
            .OrderByDescending(c => c.UltimaMensagemEmUtc)
            .ToListAsync();

    public async Task<int> TotalNaoLidasDaLocadoraAsync() =>
        await dbContext.Conversas.SumAsync(c => c.NaoLidasLocadora);

    // ---------------- mensagens ----------------

    /// <summary>As últimas mensagens da conversa, da mais antiga para a mais nova.</summary>
    public async Task<List<Mensagem>> UltimasMensagensAsync(Guid conversaId, int limite)
    {
        var ultimas = await dbContext.Mensagens.AsNoTracking()
            .Where(m => m.ConversaId == conversaId)
            .OrderByDescending(m => m.EnviadaEmUtc)
            .Take(limite)
            .ToListAsync();

        ultimas.Reverse();

        return ultimas;
    }
}
