using FluentResults;
using LocadoraDeVeiculos.Dominio.ModuloAutenticacao;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LocadoraDeVeiculos.Aplicacao.ModuloAutenticacao.Commands
{
    public record AutenticarUsuarioCommand(string Email, string Senha)
        :IRequest<Result<(AccessToken accessToken, RefreshToken RefreshToken)>>;
    
}
