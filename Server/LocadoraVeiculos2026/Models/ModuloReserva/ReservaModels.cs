using LocadoraDeVeiculos.Aplicacao.ModuloPerfilCliente.Commands;
using LocadoraDeVeiculos.Aplicacao.ModuloReserva.Commands;
using LocadoraDeVeiculos.Dominio.Compartilhado;
using LocadoraDeVeiculos.Dominio.ModuloPlanoCobranca;

namespace LocadoraDeVeiculos.WebApi.Models.ModuloReserva;

/// <summary>Datas no formato ISO (yyyy-MM-dd).</summary>
public record DadosReservaRequest(
    Guid AutomovelId,
    DateOnly Retirada,
    DateOnly Devolucao,
    TipoPlano TipoPlano,
    TipoSeguro TipoSeguro
)
{
    public DadosReservaDto ParaDados() => new(AutomovelId, Retirada, Devolucao, TipoPlano, TipoSeguro);
}

public record AprovarReservaRequest(int KmInicial);

public record RecusarReservaRequest(string? Motivo);

public record TotalDePendentesResponse(int Total);

public record SalvarMeusDadosRequest(
    string Cpf,
    string Rg,
    string Cnh,
    DateOnly ValidadeCnh,
    string Telefone,
    string Estado,
    string Cidade,
    string Bairro,
    string Rua,
    string Numero
)
{
    public DadosPerfilClienteDto ParaDados() =>
        new(Cpf, Rg, Cnh, ValidadeCnh, Telefone, Estado, Cidade, Bairro, Rua, Numero);
}
