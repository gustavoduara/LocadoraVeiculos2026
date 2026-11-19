using LocadoraDeVeiculos.Dominio.Compartilhado;
using LocadoraDeVeiculos.Dominio.ModuloGrupoVeiculos;

namespace LocadoraDeVeiculos.Dominio.ModuloPlanoCobranca;

/// <summary>
/// Plano de cobrança de um grupo de veículos. Reúne os valores das três modalidades
/// (Diário, Controlado e Livre) que o cliente poderá escolher ao alugar um veículo do grupo.
/// Há no máximo um plano de cobrança ativo por grupo.
/// </summary>
public class PlanoCobranca : EntidadeBase<PlanoCobranca>
{
    public Guid GrupoVeiculosId { get; set; }
    public GrupoVeiculos? GrupoVeiculos { get; set; }

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
        Guid grupoVeiculosId,
        decimal diarioPrecoDiaria,
        decimal diarioPrecoPorKm,
        int controladoKmDisponiveis,
        decimal controladoPrecoDiaria,
        decimal controladoPrecoPorKmExtrapolado,
        decimal livrePrecoDiaria
    )
    {
        EmpresaId = empresaId;
        GrupoVeiculosId = grupoVeiculosId;
        DiarioPrecoDiaria = diarioPrecoDiaria;
        DiarioPrecoPorKm = diarioPrecoPorKm;
        ControladoKmDisponiveis = controladoKmDisponiveis;
        ControladoPrecoDiaria = controladoPrecoDiaria;
        ControladoPrecoPorKmExtrapolado = controladoPrecoPorKmExtrapolado;
        LivrePrecoDiaria = livrePrecoDiaria;
    }

    /// <summary>Atualiza os valores; o grupo do plano não muda.</summary>
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
