using LocadoraDeVeiculos.Dominio.Compartilhado;
using LocadoraDeVeiculos.Dominio.ModuloCondutor;
using LocadoraDeVeiculos.Dominio.ModuloConfiguracao;
using LocadoraDeVeiculos.Dominio.ModuloTaxaServico;

namespace LocadoraDeVeiculos.Testes.Dominio;

public class EntidadesTestes
{
    private static readonly Guid Empresa = Guid.NewGuid();

    [Fact]
    public void Condutor_com_cnh_vencida_e_detectado_pela_data()
    {
        var condutor = new Condutor(Empresa, Guid.NewGuid(), "Carlos", "c@ex.com", "(47) 99999-1234",
            "111.444.777-35", "12345678901", new DateOnly(2026, 11, 10));

        Assert.True(condutor.CnhVencida(new DateOnly(2026, 11, 11)));
        Assert.False(condutor.CnhVencida(new DateOnly(2026, 11, 10))); // vence no próprio dia: ainda vale
        Assert.False(condutor.CnhVencida(new DateOnly(2026, 11, 9)));
    }

    [Fact]
    public void Condutor_atualizar_nao_troca_o_cliente()
    {
        var cliente = Guid.NewGuid();
        var condutor = new Condutor(Empresa, cliente, "Carlos", "c@ex.com", "(47) 99999-1234",
            "111.444.777-35", "12345678901", new DateOnly(2030, 1, 1));

        var editado = new Condutor(Empresa, Guid.NewGuid(), "Carlos Alberto", "novo@ex.com", "(47) 3333-4444",
            "111.444.777-35", "10987654321", new DateOnly(2031, 1, 1));

        condutor.AtualizarRegistro(editado);

        Assert.Equal(cliente, condutor.ClienteId);
        Assert.Equal("Carlos Alberto", condutor.Nome);
        Assert.Equal("10987654321", condutor.Cnh);
        Assert.Equal(new DateOnly(2031, 1, 1), condutor.ValidadeCnh);
    }

    [Fact]
    public void TaxaServico_atualizar_copia_os_dados()
    {
        var taxa = new TaxaServico(Empresa, "GPS", 20m, TipoCobrancaTaxa.Diario);

        taxa.AtualizarRegistro(new TaxaServico(Empresa, "Cadeirinha", 50m, TipoCobrancaTaxa.Fixo));

        Assert.Equal("Cadeirinha", taxa.Nome);
        Assert.Equal(50m, taxa.Preco);
        Assert.Equal(TipoCobrancaTaxa.Fixo, taxa.TipoCobranca);
    }

    [Fact]
    public void Configuracao_devolve_o_preco_de_cada_combustivel_e_seguro()
    {
        var config = new Configuracao(Empresa, 6m, 4m, 5.5m, 4.2m, 30m, 20m);

        Assert.Equal(6m, config.ObterPreco(TipoCombustivel.Gasolina));
        Assert.Equal(4m, config.ObterPreco(TipoCombustivel.Gas));
        Assert.Equal(5.5m, config.ObterPreco(TipoCombustivel.Diesel));
        Assert.Equal(4.2m, config.ObterPreco(TipoCombustivel.Alcool));

        Assert.Equal(0m, config.ObterPrecoSeguro(TipoSeguro.Nenhum));
        Assert.Equal(30m, config.ObterPrecoSeguro(TipoSeguro.Cliente));
        Assert.Equal(20m, config.ObterPrecoSeguro(TipoSeguro.Terceiros));
    }
}
