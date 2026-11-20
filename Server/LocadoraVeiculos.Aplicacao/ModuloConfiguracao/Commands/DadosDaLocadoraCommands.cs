using FluentResults;
using MediatR;

namespace LocadoraDeVeiculos.Aplicacao.ModuloConfiguracao.Commands;

public record DadosDaLocadoraDto(string? NomeFantasia, string? Telefone);

public record SelecionarDadosDaLocadoraQuery() : IRequest<Result<DadosDaLocadoraDto>>;

public record EditarDadosDaLocadoraCommand(string? NomeFantasia, string? Telefone) : IRequest<Result<DadosDaLocadoraDto>>;
