using FluentResults;
using LocadoraDeVeiculos.Dominio.ModuloAutenticacao;
using MediatR;
using Microsoft.Win32.SafeHandles;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LocadoraDeVeiculos.Aplicacao.ModuloAutenticacao.Commands
{
    public record RotacionarTokenCommand(string RefreshTokenString)
    : IRequest<Result<(AccessToken AccessToken, RefreshToken RefreshToken)>>;


}
