using LocadoraDeVeiculos.Infraestrutura.Orm.Compartilhado;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using static LocadoraDeVeiculos.Testes.Integracao.Cenario;

namespace LocadoraDeVeiculos.Testes.Integracao;

public class ChatIntegracaoTestes(ApiFactory fabrica) : IClassFixture<ApiFactory>
{
    private async Task<ClienteApi> NovoCliente(string nome = "Beatriz Lima")
    {
        var api = new ClienteApi(fabrica.CreateClient());

        api.UsarToken(await CriarContaEEntrar(api, "/api/auth/registrar-cliente", nome, Geradores.Email("chat")));

        return api;
    }

    private async Task<(Cenario Locadora, Guid Carro)> NovaLocadora()
    {
        var c = await Criar(fabrica);

        return (c, await c.NovoAutomovel());
    }

    private static async Task<Guid> Iniciar(ClienteApi cliente, Guid carro, string texto = "Esse carro está disponível?")
    {
        var r = await cliente.Post("/api/minhas-conversas", new { automovelId = carro, texto });
        Assert.Equal(201, r.Codigo);

        return r.Id;
    }

    [Fact]
    public async Task Cliente_inicia_a_conversa_pelo_carro_e_a_locadora_responde()
    {
        var (c, carro) = await NovaLocadora();
        var cliente = await NovoCliente();

        var id = await Iniciar(cliente, carro);

        // Chegou na caixa de entrada da locadora, com 1 não lida.
        var lista = await c.Api.Get("/api/conversas");
        Assert.Equal(200, lista.Codigo);
        var item = Assert.Single(lista.Registros.EnumerateArray());
        Assert.Equal(id, item.GetProperty("id").GetGuid());
        Assert.Equal(1, item.GetProperty("naoLidas").GetInt32());
        Assert.Equal(1, (await c.Api.Get("/api/conversas/nao-lidas/total")).Corpo.GetProperty("total").GetInt32());

        // Abrir marca como lida.
        var aberta = await c.Api.Get($"/api/conversas/{id}");
        Assert.Equal(200, aberta.Codigo);
        Assert.Single(aberta.Corpo.GetProperty("mensagens").EnumerateArray());
        Assert.Equal(0, (await c.Api.Get("/api/conversas/nao-lidas/total")).Corpo.GetProperty("total").GetInt32());

        // A locadora responde: o cliente passa a ter 1 não lida.
        Assert.Equal(201, (await c.Api.Post($"/api/conversas/{id}/mensagens", new { texto = "Está sim!" })).Codigo);
        Assert.Equal(1, (await cliente.Get("/api/minhas-conversas/nao-lidas/total")).Corpo.GetProperty("total").GetInt32());

        var doCliente = await cliente.Get($"/api/minhas-conversas/{id}");
        var mensagens = doCliente.Corpo.GetProperty("mensagens").EnumerateArray().ToList();
        Assert.Equal(2, mensagens.Count);
        Assert.Equal("Locadora", mensagens[1].GetProperty("autor").GetString());
        Assert.Equal(0, (await cliente.Get("/api/minhas-conversas/nao-lidas/total")).Corpo.GetProperty("total").GetInt32());
    }

    [Fact]
    public async Task Iniciar_de_novo_com_a_mesma_locadora_continua_a_mesma_conversa()
    {
        var (c, carro) = await NovaLocadora();
        var outroCarro = await c.NovoAutomovel();
        var cliente = await NovoCliente();

        var primeira = await Iniciar(cliente, carro);
        var segunda = await Iniciar(cliente, outroCarro, "E este outro?");

        Assert.Equal(primeira, segunda);
        Assert.Single((await cliente.Get("/api/minhas-conversas")).Registros.EnumerateArray());
        Assert.Equal(2, (await cliente.Get($"/api/minhas-conversas/{primeira}")).Corpo.GetProperty("mensagens").GetArrayLength());
    }

    [Fact]
    public async Task Funcionario_ve_e_responde_a_caixa_de_entrada_da_propria_locadora()
    {
        var c = await Criar(fabrica, comFuncionario: true);
        var carro = await c.NovoAutomovel();
        var cliente = await NovoCliente();
        var id = await Iniciar(cliente, carro);

        var funcionario = new ClienteApi(fabrica.CreateClient());
        funcionario.UsarToken(c.TokenFuncionario);

        Assert.Single((await funcionario.Get("/api/conversas")).Registros.EnumerateArray());
        Assert.Equal(201, (await funcionario.Post($"/api/conversas/{id}/mensagens", new { texto = "Oi, aqui é da locadora." })).Codigo);
    }

    [Fact]
    public async Task Conversas_nao_vazam_entre_locadoras_nem_entre_clientes()
    {
        var (a, carroA) = await NovaLocadora();
        var (b, _) = await NovaLocadora();
        var dono = await NovoCliente("Dono");
        var intruso = await NovoCliente("Intruso");

        var id = await Iniciar(dono, carroA);

        // Outra locadora não vê nem escreve na conversa.
        Assert.Empty((await b.Api.Get("/api/conversas")).Registros.EnumerateArray());
        Assert.Equal(404, (await b.Api.Get($"/api/conversas/{id}")).Codigo);
        Assert.Equal(404, (await b.Api.Post($"/api/conversas/{id}/mensagens", new { texto = "oi" })).Codigo);

        // Outro cliente também não.
        Assert.Empty((await intruso.Get("/api/minhas-conversas")).Registros.EnumerateArray());
        Assert.Equal(404, (await intruso.Get($"/api/minhas-conversas/{id}")).Codigo);
        Assert.Equal(404, (await intruso.Post($"/api/minhas-conversas/{id}/mensagens", new { texto = "oi" })).Codigo);

        // E os papéis não se misturam.
        Assert.Equal(403, (await dono.Get("/api/conversas")).Codigo);
        Assert.Equal(403, (await a.Api.Get("/api/minhas-conversas")).Codigo);
    }

    [Fact]
    public async Task Mensagem_vazia_ou_muito_longa_e_carro_inexistente_sao_recusados()
    {
        var (_, carro) = await NovaLocadora();
        var cliente = await NovoCliente();

        Assert.Equal(400, (await cliente.Post("/api/minhas-conversas", new { automovelId = carro, texto = "   " })).Codigo);
        Assert.Equal(400, (await cliente.Post("/api/minhas-conversas", new { automovelId = carro, texto = new string('a', 1001) })).Codigo);
        Assert.Equal(404, (await cliente.Post("/api/minhas-conversas", new { automovelId = Guid.NewGuid(), texto = "oi" })).Codigo);

        // O limite é de 1000 caracteres: 1000 passa.
        Assert.Equal(201, (await cliente.Post("/api/minhas-conversas", new { automovelId = carro, texto = new string('a', 1000) })).Codigo);
        Assert.Empty((await cliente.Get("/api/minhas-conversas")).Registros.EnumerateArray().Where(_ => false));
    }

    [Fact]
    public async Task Sem_login_nao_acessa_o_chat()
    {
        var anonimo = new ClienteApi(fabrica.CreateClient());

        Assert.Equal(401, (await anonimo.Get("/api/minhas-conversas")).Codigo);
        Assert.Equal(401, (await anonimo.Get("/api/conversas")).Codigo);
    }

    [Fact]
    public async Task Excesso_de_mensagens_por_minuto_e_barrado_por_usuario()
    {
        var (c, carro) = await NovaLocadora();
        var cliente = await NovoCliente();
        var outro = await NovoCliente("Outra pessoa");
        var id = await Iniciar(cliente, carro);

        bool barrou = false;

        for (int i = 0; i < 40 && !barrou; i++)
            barrou = (await cliente.Post($"/api/minhas-conversas/{id}/mensagens", new { texto = $"mensagem {i}" })).Codigo == 429;

        Assert.True(barrou);

        // Outro usuário (mesmo IP) não é afetado.
        Assert.Equal(201, (await outro.Post("/api/minhas-conversas", new { automovelId = carro, texto = "oi" })).Codigo);
    }

    [Fact]
    public async Task So_a_primeira_mensagem_nao_lida_avisa_a_locadora_por_email()
    {
        var (c, carro) = await NovaLocadora();
        var cliente = await NovoCliente();

        string emailDaLocadora;

        using (var escopo = fabrica.Services.CreateScope())
            emailDaLocadora = (await escopo.ServiceProvider.GetRequiredService<AppDbContext>().Users.IgnoreQueryFilters()
                .FirstAsync(u => u.Id == c.EmpresaId)).Email!;

        int Avisos() => EmailsDeTeste.Recebidos(emailDaLocadora)
            .Count(e => e.Assunto.Contains("mensagem", StringComparison.OrdinalIgnoreCase));

        var id = await Iniciar(cliente, carro);
        Assert.Equal(1, Avisos());

        await cliente.Post($"/api/minhas-conversas/{id}/mensagens", new { texto = "segunda" });
        await cliente.Post($"/api/minhas-conversas/{id}/mensagens", new { texto = "terceira" });
        Assert.Equal(1, Avisos());

        // Depois que a locadora lê, a próxima mensagem volta a avisar.
        await c.Api.Get($"/api/conversas/{id}");
        await cliente.Post($"/api/minhas-conversas/{id}/mensagens", new { texto = "quarta" });
        Assert.Equal(2, Avisos());
    }

    [Fact]
    public async Task Excluir_a_conta_do_cliente_apaga_as_conversas_dele()
    {
        var (c, carro) = await NovaLocadora();
        var cliente = await NovoCliente();
        var id = await Iniciar(cliente, carro);

        Assert.Equal(204, (await cliente.Post("/api/conta/excluir", new { senha = "Senha123", confirmacao = "EXCLUIR" })).Codigo);

        Assert.Empty((await c.Api.Get("/api/conversas")).Registros.EnumerateArray());
        Assert.Equal(404, (await c.Api.Get($"/api/conversas/{id}")).Codigo);

        using var escopo = fabrica.Services.CreateScope();
        var db = escopo.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.False(await db.Mensagens.AnyAsync(m => m.ConversaId == id));
    }

    [Fact]
    public async Task Mensagem_chega_em_tempo_real_para_as_duas_pontas_e_so_para_elas()
    {
        var (c, carro) = await NovaLocadora();
        var (outraLocadora, _) = await NovaLocadora();
        var cliente = await NovoCliente();

        string tokenCliente = await TokenDe(cliente);

        await using var doCliente = Conectar(tokenCliente);
        await using var daLocadora = Conectar(c.TokenEmpresa);
        await using var deOutraLocadora = Conectar(outraLocadora.TokenEmpresa);

        var recebidoCliente = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var recebidoLocadora = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var recebidoIntruso = false;

        doCliente.On<System.Text.Json.JsonElement>("MensagemRecebida", _ => recebidoCliente.TrySetResult(true));
        daLocadora.On<System.Text.Json.JsonElement>("MensagemRecebida", _ => recebidoLocadora.TrySetResult(true));
        deOutraLocadora.On<System.Text.Json.JsonElement>("MensagemRecebida", _ => recebidoIntruso = true);

        await doCliente.StartAsync();
        await daLocadora.StartAsync();
        await deOutraLocadora.StartAsync();

        await Iniciar(cliente, carro, "Olá em tempo real");

        var prazo = Task.Delay(TimeSpan.FromSeconds(10));

        Assert.True(await Task.WhenAny(Task.WhenAll(recebidoCliente.Task, recebidoLocadora.Task), prazo) != prazo);
        Assert.False(recebidoIntruso);
    }

    private static async Task<string> TokenDe(ClienteApi cliente) =>
        await Task.FromResult(cliente.TokenAtual ?? throw new InvalidOperationException("Cliente sem token."));

    private HubConnection Conectar(string token) =>
        new HubConnectionBuilder()
            .WithUrl(new Uri(fabrica.Server.BaseAddress, "/hubs/chat"), opcoes =>
            {
                opcoes.HttpMessageHandlerFactory = _ => fabrica.Server.CreateHandler();
                opcoes.Transports = HttpTransportType.LongPolling;
                opcoes.AccessTokenProvider = () => Task.FromResult<string?>(token);
            })
            .Build();
}
