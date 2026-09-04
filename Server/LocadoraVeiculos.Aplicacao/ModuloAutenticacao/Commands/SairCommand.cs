using FluentResults;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LocadoraDeVeiculos.Aplicacao.ModuloAutenticacao.Commands
{
    public record SairCommand(string RefreshTokenHash) : IRequest<Result>;
}
