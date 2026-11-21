using LocadoraDeVeiculos.Aplicacao.ModuloAluguel.Commands;
using LocadoraDeVeiculos.Aplicacao.ModuloCliente.Commands;
using LocadoraDeVeiculos.Dominio.Compartilhado;
using LocadoraDeVeiculos.Dominio.ModuloCliente;
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
    Guid? CupomId = null,
    NovoClienteNoAluguelRequest? NovoCliente = null
);

/// <summary>Condutor de um cliente novo (pessoa jurídica). Em pessoa física, só <c>ValidadeCnh</c> do cliente vale.</summary>
public record CondutorNoAluguelRequest(
    string? Nome, string? Email, string? Telefone, string? Cpf, string? Cnh, DateOnly ValidadeCnh);

/// <summary>
/// Cliente cadastrado junto com o aluguel. Pessoa física: Cpf, Rg, Cnh e ValidadeCnh (ela mesma dirige).
/// Pessoa jurídica: Cnpj e o Condutor.
/// </summary>
public record NovoClienteNoAluguelRequest(
    TipoCliente Tipo,
    string Nome,
    string Email,
    string Telefone,
    string? Cpf,
    string? Rg,
    string? Cnh,
    string? Cnpj,
    string Estado,
    string Cidade,
    string Bairro,
    string Rua,
    string Numero,
    DateOnly ValidadeCnh,
    CondutorNoAluguelRequest? Condutor = null
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
        r.CupomId,
        r.NovoCliente?.ParaDto()
    );

    private static NovoClienteNoAluguelDto ParaDto(this NovoClienteNoAluguelRequest n) => new(
        n.Tipo,
        new DadosClienteDto(
            n.Nome, n.Email, n.Telefone, n.Cpf, n.Rg, n.Cnh, n.Cnpj,
            n.Estado, n.Cidade, n.Bairro, n.Rua, n.Numero, null),
        n.Tipo == TipoCliente.PessoaFisica || n.Condutor is null
            ? new DadosCondutorNoAluguelDto(null, null, null, null, null, n.ValidadeCnh)
            : new DadosCondutorNoAluguelDto(
                n.Condutor.Nome, n.Condutor.Email, n.Condutor.Telefone, n.Condutor.Cpf, n.Condutor.Cnh,
                n.Condutor.ValidadeCnh));
}
