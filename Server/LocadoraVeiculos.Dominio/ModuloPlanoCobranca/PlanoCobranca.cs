using LocadoraDeVeiculos.Dominio.Compartilhado;
using LocadoraDeVeiculos.Dominio.ModuloAutomovel;

namespace LocadoraDeVeiculos.Dominio.ModuloPlanoCobranca;

/// <summary>
/// Plano de cobrança de um automóvel. Reúne os valores das três modalidades
/// (Diário, Controlado e Livre) que o cliente poderá escolher ao alugar aquele veículo.
/// Há no máximo um plano de cobrança ativo por automóvel.
/// </summary>
public class PlanoCobranca : EntidadeBase<PlanoCobranca>
{
    public Guid AutomovelId { get; set; }
    public Automovel? Automovel { get; set; }

    // Plano Diário: diária + valor por km rodado
    public decimal DiarioPrecoDiaria { get; set; }
    public decimal DiarioPrecoPorKm { get; set; }

    // Plano Controlado: diária com km incluídos + valor por km extrapolado
    public int ControladoKmDisponiveis { get; set; }
    public decimal ControladoPrecoDiaria { get; set; }
    public decimal ControladoPrecoPorKmExtrapolado { get; set; }

    // Plano Livre: diária com km ilimitado
    public decimal LivrePrecoDiaria { get; set; }

    protected PlanoCobranca() { }

    public PlanoCobranca(
        Guid empresaId,
        Guid automovelId,
        decimal diarioPrecoDiaria,
        decimal diarioPrecoPorKm,
        int controladoKmDisponiveis,
        decimal controladoPrecoDiaria,
        decimal controladoPrecoPorKmExtrapolado,
        decimal livrePrecoDiaria
    )
    {
        EmpresaId = empresaId;
        AutomovelId = automovelId;
        DiarioPrecoDiaria = diarioPrecoDiaria;
        DiarioPrecoPorKm = diarioPrecoPorKm;
        ControladoKmDisponiveis = controladoKmDisponiveis;
        ControladoPrecoDiaria = controladoPrecoDiaria;
        ControladoPrecoPorKmExtrapolado = controladoPrecoPorKmExtrapolado;
        LivrePrecoDiaria = livrePrecoDiaria;
    }

    /// <summary>Atualiza os valores; o automóvel do plano não muda.</summary>
    public override void AtualizarRegistro(PlanoCobranca registroEditado)
    {
        DiarioPrecoDiaria = registroEditado.DiarioPrecoDiaria;
        DiarioPrecoPorKm = registroEditado.DiarioPrecoPorKm;
        ControladoKmDisponiveis = registroEditado.ControladoKmDisponiveis;
        ControladoPrecoDiaria = registroEditado.ControladoPrecoDiaria;
        ControladoPrecoPorKmExtrapolado = registroEditado.ControladoPrecoPorKmExtrapolado;
        LivrePrecoDiaria = registroEditado.LivrePrecoDiaria;
    }
}
