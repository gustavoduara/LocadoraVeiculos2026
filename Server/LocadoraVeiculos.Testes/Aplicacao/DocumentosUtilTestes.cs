using LocadoraDeVeiculos.Aplicacao.Compartilhado;

namespace LocadoraDeVeiculos.Testes.Aplicacao;

public class DocumentosUtilTestes
{
    [Theory]
    [InlineData("111.444.777-35")]
    [InlineData("11144477735")]
    [InlineData("529.982.247-25")]
    public void Cpf_valido(string cpf) => Assert.True(DocumentosUtil.CpfValido(cpf));

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("111.444.777-36")] // dígito verificador errado
    [InlineData("000.000.000-00")] // todos iguais
    [InlineData("111.444.777")]    // curto
    [InlineData("abc")]
    public void Cpf_invalido(string? cpf) => Assert.False(DocumentosUtil.CpfValido(cpf));

    [Theory]
    [InlineData("11.222.333/0001-81")]
    [InlineData("11222333000181")]
    [InlineData("45.723.174/0001-10")]
    public void Cnpj_valido(string cnpj) => Assert.True(DocumentosUtil.CnpjValido(cnpj));

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("11.222.333/0001-82")]
    [InlineData("00.000.000/0000-00")]
    [InlineData("11.222.333/0001")]
    public void Cnpj_invalido(string? cnpj) => Assert.False(DocumentosUtil.CnpjValido(cnpj));
}
