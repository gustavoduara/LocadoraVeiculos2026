using LocadoraDeVeiculos.Dominio.Compartilhado;
using LocadoraDeVeiculos.Dominio.ModuloAutomovel;
using LocadoraDeVeiculos.Dominio.ModuloPlanoCobranca;

namespace LocadoraDeVeiculos.Dominio.ModuloReserva;

public enum StatusReserva
{
    /// <summary>O cliente pediu e a locadora ainda não respondeu.</summary>
    Pendente,

    /// <summary>A locadora aprovou: o pedido virou um aluguel em aberto.</summary>
    Aprovada,

    Recusada,

    /// <summary>O próprio cliente desistiu enquanto estava pendente.</summary>
    Cancelada
}

/// <summary>
/// Pedido de reserva feito por um cliente do catálogo. Pertence à locadora do carro (<see cref="EntidadeBase{T}.EmpresaId"/>),
/// que aprova (e o pedido vira um aluguel) ou recusa. Guarda uma cópia dos dados do carro, do preço e do cliente
/// no momento do pedido, para o histórico não mudar depois.
/// </summary>
public class Reserva : EntidadeBase<Reserva>
{
    public Guid UsuarioClienteId { get; set; }

    public Guid AutomovelId { get; set; }
    public Automovel? Automovel { get; set; }

    // ---- cópias para exibição
    public string NomeLocadora { get; set; } = string.Empty;
    public string DescricaoVeiculo { get; set; } = string.Empty;
    public string NomeGrupo { get; set; } = string.Empty;

    // ---- o que o cliente pediu
    public DateOnly Retirada { get; set; }
    public DateOnly Devolucao { get; set; }
    public TipoPlano TipoPlano { get; set; }
    public TipoSeguro TipoSeguro { get; set; }

    // ---- valor previsto no pedido (mesma regra do aluguel; km, combustível e multa só existem na devolução)
    public int Dias { get; set; }
    public decimal ValorDiarias { get; set; }
    public decimal ValorSeguro { get; set; }
    public decimal ValorCaucao { get; set; }
    public decimal ValorTotalPrevisto { get; set; }

    // ---- dados do cliente no momento do pedido
    public string ClienteNome { get; set; } = string.Empty;
    public string ClienteEmail { get; set; } = string.Empty;
    public string ClienteTelefone { get; set; } = string.Empty;
    public string ClienteCpf { get; set; } = string.Empty;
    public string ClienteRg { get; set; } = string.Empty;
    public string ClienteCnh { get; set; } = string.Empty;
    public DateOnly ClienteValidadeCnh { get; set; }
    public string ClienteEstado { get; set; } = string.Empty;
    public string ClienteCidade { get; set; } = string.Empty;
    public string ClienteBairro { get; set; } = string.Empty;
    public string ClienteRua { get; set; } = string.Empty;
    public string ClienteNumero { get; set; } = string.Empty;

    // ---- resposta
    public StatusReserva Status { get; set; } = StatusReserva.Pendente;
    public string? MotivoRecusa { get; set; }
    public Guid? AluguelId { get; set; }
    public DateTimeOffset? RespondidaEmUtc { get; set; }

    public Reserva() { }

    public bool EstaPendente => Status == StatusReserva.Pendente;

    public void Aprovar(Guid aluguelId)
    {
        Status = StatusReserva.Aprovada;
        AluguelId = aluguelId;
        RespondidaEmUtc = DateTimeOffset.UtcNow;
    }

    public void Recusar(string? motivo)
    {
        Status = StatusReserva.Recusada;
        MotivoRecusa = string.IsNullOrWhiteSpace(motivo) ? null : motivo.Trim();
        RespondidaEmUtc = DateTimeOffset.UtcNow;
    }

    public void Cancelar()
    {
        Status = StatusReserva.Cancelada;
        RespondidaEmUtc = DateTimeOffset.UtcNow;
    }

    public override void AtualizarRegistro(Reserva registroEditado)
    {
        throw new NotSupportedException("Uma reserva não é editada: o cliente cancela e pede outra.");
    }
}
