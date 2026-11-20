using LocadoraDeVeiculos.Dominio.Compartilhado;

namespace LocadoraDeVeiculos.Dominio.ModuloConfiguracao;

/// <summary>
/// Configurações da locadora (uma por empresa): preço do litro de cada combustível e preço diário
/// de cada tipo de seguro. São usados no cálculo do valor dos aluguéis.
/// </summary>
public class Configuracao : EntidadeBase<Configuracao>
{
    public decimal PrecoGasolina { get; set; }
    public decimal PrecoGas { get; set; }
    public decimal PrecoDiesel { get; set; }
    public decimal PrecoAlcool { get; set; }

    /// <summary>Valor por dia do seguro para o cliente. Zero = ainda não configurado.</summary>
    public decimal SeguroClienteDiaria { get; set; }

    /// <summary>Valor por dia do seguro para terceiros. Zero = ainda não configurado.</summary>
    public decimal SeguroTerceirosDiaria { get; set; }

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
        decimal precoAlcool,
        decimal seguroClienteDiaria,
        decimal seguroTerceirosDiaria
    )
    {
        EmpresaId = empresaId;
        PrecoGasolina = precoGasolina;
        PrecoGas = precoGas;
        PrecoDiesel = precoDiesel;
        PrecoAlcool = precoAlcool;
        SeguroClienteDiaria = seguroClienteDiaria;
        SeguroTerceirosDiaria = seguroTerceirosDiaria;
    }

    public decimal ObterPreco(TipoCombustivel combustivel) => combustivel switch
    {
        TipoCombustivel.Gasolina => PrecoGasolina,
        TipoCombustivel.Gas => PrecoGas,
        TipoCombustivel.Diesel => PrecoDiesel,
        TipoCombustivel.Alcool => PrecoAlcool,
        _ => throw new ArgumentOutOfRangeException(nameof(combustivel), combustivel, null)
    };

    /// <summary>Preço diário do seguro escolhido (0 quando não há seguro).</summary>
    public decimal ObterPrecoSeguro(TipoSeguro seguro) => seguro switch
    {
        TipoSeguro.Nenhum => 0m,
        TipoSeguro.Cliente => SeguroClienteDiaria,
        TipoSeguro.Terceiros => SeguroTerceirosDiaria,
        _ => throw new ArgumentOutOfRangeException(nameof(seguro), seguro, null)
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
        SeguroClienteDiaria = registroEditado.SeguroClienteDiaria;
        SeguroTerceirosDiaria = registroEditado.SeguroTerceirosDiaria;
    }
}
