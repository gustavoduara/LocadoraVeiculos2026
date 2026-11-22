using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using LocadoraDeVeiculos.Aplicacao.ModuloConversa;
using LocadoraDeVeiculos.Dominio.ModuloConversa;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace LocadoraDeVeiculos.WebApi.Config.Chat;

/// <summary>
/// Canal em tempo real do chat. Só recebe: quem entra é colocado no grupo certo e o servidor empurra as mensagens novas
/// (o envio é pelas rotas da API, que também gravam e validam). O cliente fica no grupo dele; a locadora (administrador e
/// funcionários) fica no grupo da empresa, então cada um só recebe o que é seu.
/// </summary>
[Authorize]
public sealed class ChatHub : Hub
{
    public static string GrupoDoCliente(Guid usuarioId) => $"usuario-{usuarioId}";

    public static string GrupoDaLocadora(Guid empresaId) => $"empresa-{empresaId}";

    public override async Task OnConnectedAsync()
    {
        ClaimsPrincipal usuario = Context.User!;

        if (usuario.IsInRole("Cliente"))
        {
            string? sub = usuario.FindFirstValue(JwtRegisteredClaimNames.Sub) ?? usuario.FindFirstValue(ClaimTypes.NameIdentifier);

            if (Guid.TryParse(sub, out Guid usuarioId))
                await Groups.AddToGroupAsync(Context.ConnectionId, GrupoDoCliente(usuarioId));
        }
        else if ((usuario.IsInRole("Empresa") || usuario.IsInRole("Funcionario")) &&
                 Guid.TryParse(usuario.FindFirstValue("EmpresaId"), out Guid empresaId))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, GrupoDaLocadora(empresaId));
        }

        await base.OnConnectedAsync();
    }
}

/// <summary>Empurra a mensagem nova para as telas abertas das duas pontas da conversa.</summary>
public sealed class ChatNotificador(IHubContext<ChatHub> hub) : IChatNotificador
{
    public Task NovaMensagemAsync(Conversa conversa, MensagemDto mensagem, CancellationToken cancellationToken) =>
        hub.Clients
            .Groups(ChatHub.GrupoDoCliente(conversa.UsuarioClienteId), ChatHub.GrupoDaLocadora(conversa.EmpresaId))
            .SendAsync("MensagemRecebida", new
            {
                conversaId = conversa.Id,
                mensagem,
                clienteNome = conversa.ClienteNome,
                nomeLocadora = conversa.NomeLocadora,
                ultimaMensagem = conversa.UltimaMensagemResumo
            }, cancellationToken);
}
