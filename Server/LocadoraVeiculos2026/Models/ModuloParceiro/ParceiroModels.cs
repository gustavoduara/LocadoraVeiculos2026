using LocadoraDeVeiculos.Aplicacao.ModuloParceiro.Commands;

namespace LocadoraDeVeiculos.WebApi.Models.ModuloParceiro;

public record CadastrarParceiroRequest(string Nome);

public record CadastrarParceiroResponse(Guid Id);

public record EditarParceiroRequest(string Nome);

public record EditarParceiroResponse(string Nome);

public record SelecionarParceirosResponse(IReadOnlyList<ParceiroDto> Registros);
