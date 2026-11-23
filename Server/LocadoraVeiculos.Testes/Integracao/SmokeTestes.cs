namespace LocadoraDeVeiculos.Testes.Integracao;

public class SmokeTestes(ApiFactory fabrica) : IClassFixture<ApiFactory>
{
    [Fact]
    public async Task Cenario_base_e_criado_com_todos_os_cadastros()
    {
        var c = await Cenario.Criar(fabrica, comFuncionario: true);

        await c.NovoAutomovel();

        Assert.NotEqual(Guid.Empty, c.PlanoId);
        Assert.NotNull(c.TokenFuncionario);
    }
}
