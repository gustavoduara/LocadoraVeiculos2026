using LocadoraDeVeiculos.Dominio.Compartilhado;

namespace LocadoraDeVeiculos.Dominio.ModuloConfiguracao;

/// <summary>
/// Configurações da locadora (uma por empresa): preço do litro de cada combustível (usado no
/// reabastecimento cobrado na devolução). O seguro é definido em cada carro.
/// </summary>
public class Configuracao : EntidadeBase<Configuracao>
{
    public decimal PrecoGasolina { get; set; }
    public decimal PrecoGas { get; set; }
    public decimal PrecoDiesel { get; set; }
    public decimal PrecoAlcool { get; set; }

    /// <summary>Nome da locadora mostrado aos clientes (no catálogo e nas reservas). Vazio = usa o nome da conta.</summary>
    public string? NomeFantasia { get; set; }

    /// <summary>Telefone de contato da locadora, informado ao cliente quando a reserva é aprovada.</summary>
    public string? Telefone { get; set; }

    protected Configuracao() { }

    public Configuracao(
        Guid empresaId,
        decimal precoGasolina,
        decimal precoGas,
        decimal precoDiesel,
        decimal precoAlcool
    )
    {
        EmpresaId = empresaId;
        PrecoGasolina = precoGasolina;
        PrecoGas = precoGas;
        PrecoDiesel = precoDiesel;
        PrecoAlcool = precoAlcool;
    }

    public decimal ObterPreco(TipoCombustivel combustivel) => combustivel switch
    {
        TipoCombustivel.Gasolina => PrecoGasolina,
        TipoCombustivel.Gas => PrecoGas,
        TipoCombustivel.Diesel => PrecoDiesel,
        TipoCombustivel.Alcool => PrecoAlcool,
        _ => throw new ArgumentOutOfRangeException(nameof(combustivel), combustivel, null)
    };

    public void AtualizarDadosDaLocadora(string? nomeFantasia, string? telefone)
    {
        NomeFantasia = string.IsNullOrWhiteSpace(nomeFantasia) ? null : nomeFantasia.Trim();
        Telefone = string.IsNullOrWhiteSpace(telefone) ? null : telefone.Trim();
    }

    public override void AtualizarRegistro(Configuracao registroEditado)
    {
        PrecoGasolina = registroEditado.PrecoGasolina;
        PrecoGas = registroEditado.PrecoGas;
        PrecoDiesel = registroEditado.PrecoDiesel;
        PrecoAlcool = registroEditado.PrecoAlcool;
    }
}
