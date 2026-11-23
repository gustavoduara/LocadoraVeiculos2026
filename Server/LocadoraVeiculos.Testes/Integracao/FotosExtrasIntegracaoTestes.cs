using System.Net;
using static LocadoraDeVeiculos.Testes.Integracao.Cenario;

namespace LocadoraDeVeiculos.Testes.Integracao;

/// <summary>Carrossel: foto principal (capa) + até 5 fotos extras por carro.</summary>
public class FotosExtrasIntegracaoTestes(ApiFactory fabrica) : IClassFixture<ApiFactory>
{
    // PNG de 1x1 pixel e uma segunda imagem diferente (para saber qual veio em cada posição).
    private static readonly byte[] PngA = Convert.FromBase64String(
        "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mP8z8BQDwAEhQGAhKmMIQAAAABJRU5ErkJggg==");

    private static byte[] PngB() => [.. PngA, 0x00, 0x01];

    private async Task<ClienteApi> NovoCliente()
    {
        var api = new ClienteApi(fabrica.CreateClient());

        api.UsarToken(await CriarContaEEntrar(api, "/api/auth/registrar-cliente", "Cliente Fotos", Geradores.Email("fotos")));

        return api;
    }

    private static async Task<Guid> AdicionarExtra(Cenario c, Guid carro, byte[] bytes, string nome = "extra.png")
    {
        var r = await c.Api.EnviarArquivo($"/api/automoveis/{carro}/fotos", bytes, nome, "image/png", post: true);
        Assert.Equal(201, (int)r.Status);

        return r.Id;
    }

    [Fact]
    public async Task Carro_pode_ter_varias_fotos_extras_e_o_catalogo_conta_e_entrega_cada_posicao()
    {
        var c = await Criar(fabrica);
        var carro = await c.NovoAutomovel();
        var cliente = await NovoCliente();

        // Sem foto nenhuma: nada para o carrossel.
        Assert.Equal(HttpStatusCode.NotFound, (await cliente.BaixarArquivo($"/api/catalogo/{carro}/fotos/0")).Status);

        Assert.Equal(200, (await c.Api.EnviarArquivo($"/api/automoveis/{carro}/foto", PngA, "capa.png", "image/png")).Codigo);

        var b = PngB();
        var extra1 = await AdicionarExtra(c, carro, b, "lado.png");
        await AdicionarExtra(c, carro, PngA, "tras.png");

        var lista = await c.Api.Get($"/api/automoveis/{carro}/fotos");
        Assert.Equal(200, lista.Codigo);
        Assert.Equal(2, lista.Registros.GetArrayLength());
        Assert.Equal(extra1, lista.Registros[0].GetProperty("id").GetGuid());

        // O catálogo informa o total (capa + 2 extras) e entrega cada posição.
        var detalhe = await cliente.Get($"/api/catalogo/{carro}");
        Assert.Equal(3, detalhe.Corpo.GetProperty("quantidadeFotos").GetInt32());

        var posicao0 = await cliente.BaixarArquivo($"/api/catalogo/{carro}/fotos/0");
        var posicao1 = await cliente.BaixarArquivo($"/api/catalogo/{carro}/fotos/1");
        Assert.Equal(PngA, posicao0.Bytes);
        Assert.Equal(b, posicao1.Bytes);
        Assert.Equal(HttpStatusCode.NotFound, (await cliente.BaixarArquivo($"/api/catalogo/{carro}/fotos/3")).Status);

        // A foto antiga do catálogo continua sendo a primeira.
        Assert.Equal(PngA, (await cliente.BaixarArquivo($"/api/catalogo/{carro}/foto")).Bytes);

        // Remover uma extra tira da galeria.
        Assert.Equal(204, (await c.Api.Delete($"/api/automoveis/{carro}/fotos/{extra1}")).Codigo);
        Assert.Equal(2, (await cliente.Get($"/api/catalogo/{carro}")).Corpo.GetProperty("quantidadeFotos").GetInt32());
    }

    [Fact]
    public async Task Carro_sem_foto_principal_usa_a_primeira_extra_como_capa()
    {
        var c = await Criar(fabrica);
        var carro = await c.NovoAutomovel();
        var cliente = await NovoCliente();

        var b = PngB();
        await AdicionarExtra(c, carro, b);

        Assert.Equal(b, (await cliente.BaixarArquivo($"/api/catalogo/{carro}/foto")).Bytes);
        Assert.Equal(1, (await cliente.Get($"/api/catalogo/{carro}")).Corpo.GetProperty("quantidadeFotos").GetInt32());
    }

    [Fact]
    public async Task Limite_de_cinco_extras_arquivo_falso_e_outra_locadora_sao_recusados()
    {
        var c = await Criar(fabrica);
        var outra = await Criar(fabrica);
        var carro = await c.NovoAutomovel();

        for (int i = 0; i < 5; i++)
            await AdicionarExtra(c, carro, PngA);

        var sexta = await c.Api.EnviarArquivo($"/api/automoveis/{carro}/fotos", PngA, "mais.png", "image/png", post: true);
        Assert.Equal(422, sexta.Codigo);

        var falsa = await c.Api.EnviarArquivo($"/api/automoveis/{carro}/fotos", "nao sou imagem"u8.ToArray(), "x.png", "image/png", post: true);
        Assert.Equal(400, falsa.Codigo);

        var primeira = (await c.Api.Get($"/api/automoveis/{carro}/fotos")).Registros[0].GetProperty("id").GetGuid();

        // Outra locadora não vê, não baixa, não envia nem remove fotos deste carro.
        Assert.Equal(404, (await outra.Api.Get($"/api/automoveis/{carro}/fotos")).Codigo);
        Assert.Equal(HttpStatusCode.NotFound, (await outra.Api.BaixarArquivo($"/api/automoveis/{carro}/fotos/{primeira}")).Status);
        Assert.Equal(404, (await outra.Api.EnviarArquivo($"/api/automoveis/{carro}/fotos", PngA, "x.png", "image/png", post: true)).Codigo);
        Assert.Equal(404, (await outra.Api.Delete($"/api/automoveis/{carro}/fotos/{primeira}")).Codigo);

        // Sem login, nada.
        Assert.Equal(HttpStatusCode.Unauthorized, (await new ClienteApi(fabrica.CreateClient()).BaixarArquivo($"/api/catalogo/{carro}/fotos/0")).Status);
    }
}
