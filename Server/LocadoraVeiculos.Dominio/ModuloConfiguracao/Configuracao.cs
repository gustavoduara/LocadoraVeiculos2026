using LocadoraDeVeiculos.Dominio.Compartilhado;

namespace LocadoraDeVeiculos.Dominio.ModuloConfiguracao;

/// <summary>
/// Configurações da locadora (uma por empresa). Hoje guarda o preço do litro de cada
/// combustível, usado no cálculo do valor dos aluguéis.
/// </summary>
public class Configuracao : EntidadeBase<Configuracao>
{
    public decimal PrecoGasolina { get; set; }
    public decimal PrecoGas { get; set; }
    public decimal PrecoDiesel { get; set; }
    public decimal PrecoAlcool { get; set; }

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

    public override void AtualizarRegistro(Configuracao registroEditado)
    {
        PrecoGasolina = registroEditado.PrecoGasolina;
        PrecoGas = registroEditado.PrecoGas;
        PrecoDiesel = registroEditado.PrecoDiesel;
        PrecoAlcool = registroEditado.PrecoAlcool;
    }
}
