using LocadoraDeVeiculos.Aplicacao.ModuloConta;
using LocadoraDeVeiculos.Dominio.ModuloAutenticacao;
using LocadoraDeVeiculos.Infraestrutura.Orm.Compartilhado;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using static LocadoraDeVeiculos.Testes.Integracao.Cenario;

namespace LocadoraDeVeiculos.Testes.Integracao;

public class ExclusaoDeContaIntegracaoTestes(ApiFactory fabrica) : IClassFixture<ApiFactory>
{
    private const string Anonimo = "Conta excluída";

    // ---------------- apoio ----------------

    private static object Dados(string cpf) => new
    {
        cpf, rg = "1234567", cnh = "12345678901", validadeCnh = Dia(1000), telefone = "(47) 99999-1234",
        estado = "SC", cidade = "Rio do Sul", bairro = "Centro", rua = "Rua XV", numero = "10"
    };

    private static object Pedido(Guid carro, int retirada = 1, int devolucao = 4) => new
    {
        automovelId = carro, retirada = Dia(retirada), devolucao = Dia(devolucao), tipoPlano = "Diario", tipoSeguro = "Nenhum"
    };

    /// <summary>Uma pessoa com conta de cliente e "Meus dados" preenchidos.</summary>
    private async Task<(ClienteApi Api, string Email, string Cpf)> NovoCliente()
    {
        var api = new ClienteApi(fabrica.CreateClient());
        var email = Geradores.Email("excluir");
        var cpf = Geradores.Cpf();

        api.UsarToken(await CriarContaEEntrar(api, "/api/auth/registrar-cliente", "Beatriz Lima", email));
        Assert.Equal(200, (await api.Put("/api/meus-dados", Dados(cpf))).Codigo);

        return (api, email, cpf);
    }

    private async Task<T> NoBanco<T>(Func<AppDbContext, UserManager<Usuario>, Task<T>> consulta)
    {
        using var escopo = fabrica.Services.CreateScope();

        return await consulta(
            escopo.ServiceProvider.GetRequiredService<AppDbContext>(),
            escopo.ServiceProvider.GetRequiredService<UserManager<Usuario>>());
    }

    private async Task<int> ExecutarAgendadas(DateTime agora)
    {
        using var escopo = fabrica.Services.CreateScope();

        return await escopo.ServiceProvider.GetRequiredService<ContaServico>().ExecutarAgendadasAsync(agora, CancellationToken.None);
    }

    /// <summary>O cliente pede, a locadora aprova: vira um aluguel em aberto.</summary>
    private static async Task<Guid> ReservaAprovada(Cenario c, ClienteApi cliente, Guid carro)
    {
        Assert.Equal(201, (await cliente.Post("/api/minhas-reservas", Pedido(carro))).Codigo);

        var id = (await c.Api.Get("/api/reservas?status=Pendente")).Registros.EnumerateArray()
            .Last(r => r.GetProperty("automovelId").GetGuid() == carro).GetProperty("id").GetGuid();

        Assert.Equal(200, (await c.Api.Post($"/api/reservas/{id}/aprovar", new { kmInicial = 1000 })).Codigo);

        return id;
    }

    private async Task<Guid> AluguelDaReserva(Cenario c, Guid reservaId) =>
        (await c.Api.Get("/api/reservas")).Registros.EnumerateArray()
            .First(r => r.GetProperty("id").GetGuid() == reservaId).GetProperty("aluguelId").GetGuid();

    // ---------------- cliente ----------------

    [Fact]
    public async Task Cliente_so_exclui_depois_de_concluir_o_aluguel_e_a_conta_some_sem_deixar_dados_pessoais()
    {
        var c = await Criar(fabrica);
        var carro = await c.NovoAutomovel();
        var (cliente, email, cpf) = await NovoCliente();

        var reserva = await ReservaAprovada(c, cliente, carro);
        var aluguel = await AluguelDaReserva(c, reserva);

        // Com o aluguel em aberto, a exclusão é barrada.
        var barrada = await cliente.Post("/api/conta/excluir", new { senha = "Senha123", confirmacao = "EXCLUIR" });
        Assert.Equal(422, barrada.Codigo);
        Assert.Contains("aluguel em aberto", barrada.Bruto);

        Assert.Equal(200, (await c.Api.Post($"/api/alugueis/{aluguel}/devolucao",
            new { dataDevolucao = Dia(4), kmFinal = 1100, tanqueCheio = true })).Codigo);

        // Senha errada e confirmação errada são recusadas e nada muda.
        Assert.Equal(400, (await cliente.Post("/api/conta/excluir", new { senha = "Errada123", confirmacao = "EXCLUIR" })).Codigo);
        Assert.Equal(400, (await cliente.Post("/api/conta/excluir", new { senha = "Senha123", confirmacao = "talvez" })).Codigo);
        Assert.Equal(200, (await cliente.Get("/api/meus-dados")).Codigo);

        // Excluir de verdade (a palavra vale em minúsculas).
        Assert.Equal(204, (await cliente.Post("/api/conta/excluir", new { senha = "Senha123", confirmacao = "excluir" })).Codigo);

        // A sessão acabou, a conta não entra mais e o e-mail ficou livre para um novo cadastro.
        Assert.Equal(401, (await cliente.Get("/api/meus-dados")).Codigo);
        var login = NovaApi();
        Assert.Equal(400, (await login.Post("/api/auth/autenticar", new { email, senha = "Senha123" })).Codigo);

        // "Meus dados" foi apagado, e a conta de login ficou anonimizada.
        var (perfis, conta) = await NoBanco(async (db, usuarios) =>
        {
            var donoDaReserva = await db.Reservas.IgnoreQueryFilters().Where(r => r.Id == reserva).Select(r => r.UsuarioClienteId).SingleAsync();
            var usuario = await db.Users.SingleAsync(u => u.Id == donoDaReserva);

            return (await db.PerfisClientes.CountAsync(p => p.UsuarioId == usuario.Id), usuario);
        });

        Assert.Equal(0, perfis);
        Assert.StartsWith("excluido-", conta.Email);
        Assert.Null(conta.PasswordHash);
        Assert.Equal(DateTimeOffset.MaxValue, conta.LockoutEnd);

        // A locadora continua com o histórico (datas, carro, valores), mas sem os dados pessoais.
        var vista = (await c.Api.Get("/api/reservas")).Registros.EnumerateArray()
            .First(r => r.GetProperty("id").GetGuid() == reserva);

        Assert.Equal(Anonimo, vista.GetProperty("clienteNome").GetString());
        Assert.Equal("", vista.GetProperty("clienteCpf").GetString());
        Assert.Equal("", vista.GetProperty("clienteEmail").GetString());
        Assert.True(vista.GetProperty("dias").GetInt32() > 0);

        var registroDoAluguel = await c.Api.Get($"/api/alugueis/{aluguel}");
        Assert.Equal(Anonimo, registroDoAluguel.Texto("nomeCliente"));
        Assert.Equal(Anonimo, registroDoAluguel.Texto("nomeCondutor"));
        Assert.DoesNotContain(cpf, registroDoAluguel.Bruto);

        var clienteDaLocadora = await c.Api.Get($"/api/clientes/{registroDoAluguel.Corpo.GetProperty("clienteId").GetGuid()}");
        Assert.Equal(Anonimo, clienteDaLocadora.Texto("nome"));
        Assert.DoesNotContain(cpf, clienteDaLocadora.Bruto);

        // Quem excluiu pode voltar com o mesmo e-mail.
        var novo = await login.Post("/api/auth/registrar-cliente",
            new { nomeCompleto = "Beatriz Lima", email, senha = "Senha123", confirmarSenha = "Senha123", aceitouTermos = true });
        Assert.Equal(200, novo.Codigo);
    }

    [Fact]
    public async Task Pedidos_pendentes_sao_cancelados_quando_o_cliente_exclui_a_conta()
    {
        var c = await Criar(fabrica);
        var carro = await c.NovoAutomovel();
        var (cliente, _, _) = await NovoCliente();

        Assert.Equal(201, (await cliente.Post("/api/minhas-reservas", Pedido(carro))).Codigo);

        Assert.Equal(204, (await cliente.Post("/api/conta/excluir", new { senha = "Senha123", confirmacao = "EXCLUIR" })).Codigo);

        var pedido = (await c.Api.Get("/api/reservas")).Registros.EnumerateArray().Single();

        Assert.Equal("Cancelada", pedido.GetProperty("status").GetString());
        Assert.Equal(Anonimo, pedido.GetProperty("clienteNome").GetString());
        Assert.Equal(0, (await c.Api.Get("/api/reservas/pendentes/total")).Corpo.GetProperty("total").GetInt32());
    }

    [Fact]
    public async Task Excluir_conta_de_cliente_e_so_para_clientes_e_so_logado()
    {
        var c = await Criar(fabrica, comFuncionario: true);

        Assert.Equal(403, (await c.Api.Post("/api/conta/excluir", new { senha = "Senha123", confirmacao = "EXCLUIR" })).Codigo);

        c.Api.UsarToken(c.TokenFuncionario);
        Assert.Equal(403, (await c.Api.Post("/api/conta/excluir", new { senha = "Senha123", confirmacao = "EXCLUIR" })).Codigo);

        Assert.Equal(401, (await NovaApi().Post("/api/conta/excluir", new { senha = "Senha123", confirmacao = "EXCLUIR" })).Codigo);
    }

    // ---------------- locadora ----------------

    private ClienteApi NovaApi() => new(fabrica.CreateClient());

    /// <summary>O nome da conta é o do cadastro: <see cref="Cenario.RegistrarEmpresa"/> usa este valor padrão.</summary>
    private const string NomeDaEmpresa = "Empresa Teste";

    [Fact]
    public async Task Locadora_agenda_a_exclusao_para_7_dias_e_pode_cancelar()
    {
        var c = await Criar(fabrica);
        var nome = NomeDaEmpresa;

        // Senha errada, nome que não confere.
        Assert.Equal(400, (await c.Api.Post("/api/conta/agendar-exclusao", new { senha = "Errada123", nomeDaEmpresa = nome })).Codigo);
        Assert.Equal(400, (await c.Api.Post("/api/conta/agendar-exclusao", new { senha = "Senha123", nomeDaEmpresa = "Outra Empresa" })).Codigo);
        Assert.Equal(System.Text.Json.JsonValueKind.Null, (await c.Api.Get("/api/conta/situacao")).Corpo.GetProperty("exclusaoAgendadaParaUtc").ValueKind);

        var agendada = await c.Api.Post("/api/conta/agendar-exclusao", new { senha = "Senha123", nomeDaEmpresa = nome.ToUpperInvariant() });
        Assert.Equal(200, agendada.Codigo);

        var data = agendada.Corpo.GetProperty("exclusaoAgendadaParaUtc").GetDateTime();
        Assert.InRange(data, DateTime.UtcNow.AddDays(6.9), DateTime.UtcNow.AddDays(7.1));

        // A conta continua entrando, e a situação mostra a data.
        var situacao = await c.Api.Get("/api/conta/situacao");
        Assert.Equal(200, situacao.Codigo);
        Assert.Equal(data, situacao.Corpo.GetProperty("exclusaoAgendadaParaUtc").GetDateTime());

        // Não agenda duas vezes.
        Assert.Equal(422, (await c.Api.Post("/api/conta/agendar-exclusao", new { senha = "Senha123", nomeDaEmpresa = nome })).Codigo);

        // O e-mail avisando chegou, com a data.
        Assert.Contains(EmailsDeTeste.Recebidos(await EmailDaEmpresa(c)), e => e.Assunto.Contains("agendada") && e.Corpo.Contains(data.ToString("dd/MM/yyyy")));

        Assert.Equal(204, (await c.Api.Post("/api/conta/cancelar-exclusao")).Codigo);
        Assert.Equal(System.Text.Json.JsonValueKind.Null, (await c.Api.Get("/api/conta/situacao")).Corpo.GetProperty("exclusaoAgendadaParaUtc").ValueKind);
        Assert.Equal(422, (await c.Api.Post("/api/conta/cancelar-exclusao")).Codigo);

        // Cancelada: o prazo passando não apaga nada.
        Assert.Equal(0, await ExecutarAgendadas(DateTime.UtcNow.AddDays(30)));
    }

    private async Task<string> EmailDaEmpresa(Cenario c) =>
        await NoBanco(async (db, _) => (await db.Users.SingleAsync(u => u.Id == c.EmpresaId)).Email!);

    [Fact]
    public async Task Exclusao_da_locadora_e_barrada_com_aluguel_em_aberto_ou_reserva_pendente()
    {
        var c = await Criar(fabrica);
        var nome = NomeDaEmpresa;
        var carro = await c.NovoAutomovel();

        Assert.Equal(201, (await c.Api.Post("/api/alugueis", PedidoAluguel(c.CondutorId, carro, seguro: "Nenhum"))).Codigo);

        var comAluguel = await c.Api.Post("/api/conta/agendar-exclusao", new { senha = "Senha123", nomeDaEmpresa = nome });
        Assert.Equal(422, comAluguel.Codigo);
        Assert.Contains("aluguéis em aberto", comAluguel.Bruto);

        // Concluído o aluguel, uma reserva esperando resposta também impede.
        var aluguel = (await c.Api.Get("/api/alugueis?status=EmAberto")).Registros[0].GetProperty("id").GetGuid();
        Assert.Equal(200, (await c.Api.Post($"/api/alugueis/{aluguel}/devolucao", new { dataDevolucao = Dia(3), kmFinal = 1100, tanqueCheio = true })).Codigo);

        var (cliente, _, _) = await NovoCliente();
        var outroCarro = await c.NovoAutomovel();
        Assert.Equal(201, (await cliente.Post("/api/minhas-reservas", Pedido(outroCarro))).Codigo);

        var comReserva = await c.Api.Post("/api/conta/agendar-exclusao", new { senha = "Senha123", nomeDaEmpresa = nome });
        Assert.Equal(422, comReserva.Codigo);
        Assert.Contains("reservas esperando resposta", comReserva.Bruto);
    }

    [Fact]
    public async Task Funcionario_e_cliente_nao_agendam_a_exclusao_da_locadora()
    {
        var c = await Criar(fabrica, comFuncionario: true);
        var (cliente, _, _) = await NovoCliente();

        c.Api.UsarToken(c.TokenFuncionario);
        Assert.Equal(403, (await c.Api.Post("/api/conta/agendar-exclusao", new { senha = "Senha123", nomeDaEmpresa = "Empresa Teste" })).Codigo);
        Assert.Equal(403, (await cliente.Post("/api/conta/agendar-exclusao", new { senha = "Senha123", nomeDaEmpresa = "Empresa Teste" })).Codigo);
    }

    [Fact]
    public async Task Passado_o_prazo_a_locadora_some_e_os_dados_pessoais_sao_anonimizados()
    {
        var c = await Criar(fabrica, comFuncionario: true);
        var nome = NomeDaEmpresa;
        var email = await EmailDaEmpresa(c);
        var carro = await c.NovoAutomovel();

        Assert.Equal(200, (await c.Api.Post("/api/conta/agendar-exclusao", new { senha = "Senha123", nomeDaEmpresa = nome })).Codigo);

        // Antes do prazo nada acontece.
        Assert.Equal(0, await ExecutarAgendadas(DateTime.UtcNow.AddDays(3)));
        Assert.Equal(200, (await c.Api.Get("/api/grupos-veiculos")).Codigo);

        // Depois do prazo, a exclusão é executada.
        Assert.Equal(1, await ExecutarAgendadas(DateTime.UtcNow.AddDays(8)));

        // Ninguém da empresa entra mais, nem o administrador nem o funcionário, e as sessões abertas caem.
        Assert.Equal(401, (await c.Api.Get("/api/grupos-veiculos")).Codigo);
        Assert.Equal(400, (await NovaApi().Post("/api/auth/autenticar", new { email, senha = "Senha123" })).Codigo);

        var (empresa, restantes, anonimizados, funcionariosSemAcesso) = await NoBanco(async (db, _) =>
        {
            var e = await db.Users.SingleAsync(u => u.Id == c.EmpresaId);
            var clientes = await db.Clientes.IgnoreQueryFilters().Where(x => x.EmpresaId == c.EmpresaId).ToListAsync();
            var funcionarios = await db.Funcionarios.IgnoreQueryFilters().Where(x => x.EmpresaId == c.EmpresaId).ToListAsync();
            var contas = await db.Users.Where(u => funcionarios.Select(f => f.UsuarioId).Contains(u.Id)).ToListAsync();

            return (e,
                    clientes.Count(x => !x.Excluido) + await db.Automoveis.IgnoreQueryFilters().CountAsync(x => x.EmpresaId == c.EmpresaId && !x.Excluido),
                    clientes.All(x => x.Nome == Anonimo) && funcionarios.All(f => f.NomeCompleto == Anonimo),
                    contas.All(u => u.ContaExcluidaEmUtc != null && u.PasswordHash == null));
        });

        Assert.Equal(Anonimo, empresa.FullName);
        Assert.NotNull(empresa.ContaExcluidaEmUtc);
        Assert.Null(empresa.ExclusaoAgendadaParaUtc);
        Assert.Equal(0, restantes);
        Assert.True(anonimizados);
        Assert.True(funcionariosSemAcesso);

        // Os carros da locadora saíram do catálogo.
        var cliente = NovaApi();
        cliente.UsarToken(await CriarContaEEntrar(cliente, "/api/auth/registrar-cliente", "Quem Procura", Geradores.Email("catalogo")));
        Assert.Equal(0, (await cliente.Get($"/api/catalogo?locadoraId={c.EmpresaId}&apenasDisponiveis=false")).Corpo.GetProperty("total").GetInt32());

        // O e-mail da empresa pode ser usado de novo e a pessoa foi avisada.
        Assert.Contains(EmailsDeTeste.Recebidos(email), e => e.Assunto.Contains("foi excluída"));
        Assert.Equal(200, (await NovaApi().Post("/api/auth/registrar",
            new { nomeCompleto = "Empresa Nova", email, senha = "Senha123", confirmarSenha = "Senha123", aceitouTermos = true })).Codigo);
    }

    [Fact]
    public async Task Se_aparecer_um_aluguel_em_aberto_depois_do_pedido_a_exclusao_e_adiada()
    {
        var c = await Criar(fabrica);
        var nome = NomeDaEmpresa;
        var carro = await c.NovoAutomovel();

        Assert.Equal(200, (await c.Api.Post("/api/conta/agendar-exclusao", new { senha = "Senha123", nomeDaEmpresa = nome })).Codigo);

        // Durante o prazo a locadora segue trabalhando e fecha um aluguel.
        Assert.Equal(201, (await c.Api.Post("/api/alugueis", PedidoAluguel(c.CondutorId, carro, seguro: "Nenhum"))).Codigo);

        var agora = DateTime.UtcNow.AddDays(8);
        Assert.Equal(0, await ExecutarAgendadas(agora));

        var empresa = await NoBanco((db, _) => db.Users.SingleAsync(u => u.Id == c.EmpresaId));
        Assert.Null(empresa.ContaExcluidaEmUtc);
        Assert.NotNull(empresa.ExclusaoAgendadaParaUtc);
        Assert.True(empresa.ExclusaoAgendadaParaUtc > agora);

        // Os dados continuam intactos.
        Assert.Equal(200, (await c.Api.Get("/api/alugueis")).Codigo);
        Assert.NotEmpty(EmailsDeTeste.Recebidos(empresa.Email!).Where(e => e.Assunto.Contains("adiada")));
    }
}
