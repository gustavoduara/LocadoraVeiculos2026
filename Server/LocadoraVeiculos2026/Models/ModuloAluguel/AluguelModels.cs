using LocadoraDeVeiculos.Aplicacao.ModuloAluguel.Commands;
using LocadoraDeVeiculos.Dominio.Compartilhado;
using LocadoraDeVeiculos.Dominio.ModuloPlanoCobranca;

namespace LocadoraDeVeiculos.WebApi.Models.ModuloAluguel;

/// <summary>Datas no formato ISO (yyyy-MM-dd). <c>TaxasIds</c> pode ser vazia.</summary>
public record DadosAluguelRequest(
    Guid CondutorId,
    Guid AutomovelId,
    TipoPlano TipoPlano,
    DateOnly DataSaida,
    DateOnly DataRetornoPrevista,
    int KmInicial,
    TipoSeguro TipoSeguro,
    List<Guid>? TaxasIds,
    Guid? CupomId = null
);

public record CadastrarAluguelResponse(Guid Id);

public record DevolucaoRequest(DateOnly DataDevolucao, int KmFinal, bool TanqueCheio);

public record SelecionarAlugueisResponse(IReadOnlyList<AluguelDto> Registros);

public static class AluguelRequestExtensoes
{
    public static DadosAluguelDto ParaDados(this DadosAluguelRequest r) => new(
        r.CondutorId,
        r.AutomovelId,
        r.TipoPlano,
        r.DataSaida,
        r.DataRetornoPrevista,
        r.KmInicial,
        r.TipoSeguro,
        r.TaxasIds ?? [],
        r.CupomId
    );
}
