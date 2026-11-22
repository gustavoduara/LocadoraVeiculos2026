using LocadoraDeVeiculos.Dominio.Compartilhado;

namespace LocadoraDeVeiculos.Dominio.ModuloConversa;

public enum AutorMensagem
{
    /// <summary>Quem procura o carro (conta de cliente).</summary>
    Cliente,

    /// <summary>A locadora: o administrador ou um funcionário.</summary>
    Locadora
}

/// <summary>
/// Conversa entre um cliente e uma locadora (uma por par). Pertence à locadora (<see cref="EntidadeBase{T}.EmpresaId"/>), então
/// o administrador e os funcionários dela compartilham a mesma caixa de entrada; o cliente enxerga só as suas.
/// </summary>
public class Conversa : EntidadeBase<Conversa>
{
    public Guid UsuarioClienteId { get; set; }

    // ---- cópias para exibição (a conta do cliente pode ser excluída e a locadora pode mudar de nome)
    public string ClienteNome { get; set; } = string.Empty;
    public string NomeLocadora { get; set; } = string.Empty;

    public DateTimeOffset UltimaMensagemEmUtc { get; set; } = DateTimeOffset.UtcNow;
    public string UltimaMensagemResumo { get; set; } = string.Empty;

    /// <summary>Mensagens da locadora que o cliente ainda não abriu.</summary>
    public int NaoLidasCliente { get; set; }

    /// <summary>Mensagens do cliente que a locadora ainda não abriu.</summary>
    public int NaoLidasLocadora { get; set; }

    public List<Mensagem> Mensagens { get; set; } = [];

    public Conversa() { }

    public override void AtualizarRegistro(Conversa registroEditado) =>
        throw new NotSupportedException("Uma conversa não é editada: ela só recebe mensagens.");
}

/// <summary>Uma mensagem. Quando a conversa começa por um carro, a mensagem guarda de qual carro se trata.</summary>
public class Mensagem
{
    public const int TamanhoMaximo = 1000;

    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid ConversaId { get; set; }

    public AutorMensagem Autor { get; set; }
    public string AutorNome { get; set; } = string.Empty;

    public string Texto { get; set; } = string.Empty;
    public DateTimeOffset EnviadaEmUtc { get; set; } = DateTimeOffset.UtcNow;

    public Guid? AutomovelId { get; set; }
    public string? DescricaoVeiculo { get; set; }

    public Mensagem() { }
}
