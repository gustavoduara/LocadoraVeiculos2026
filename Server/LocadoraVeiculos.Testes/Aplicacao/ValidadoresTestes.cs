using FluentValidation.TestHelper;
using LocadoraDeVeiculos.Aplicacao.ModuloAluguel.Commands;
using LocadoraDeVeiculos.Aplicacao.ModuloAluguel.Validators;
using LocadoraDeVeiculos.Aplicacao.ModuloCliente.Commands;
using LocadoraDeVeiculos.Aplicacao.ModuloCliente.Validators;
using LocadoraDeVeiculos.Dominio.Compartilhado;
using LocadoraDeVeiculos.Dominio.ModuloPlanoCobranca;

namespace LocadoraDeVeiculos.Testes.Aplicacao;

public class ValidadoresTestes
{
    // ---------------- Cliente ----------------

    private static DadosClienteDto PessoaFisica(string? cpf = "111.444.777-35", string? cnh = "12345678901") => new(
        "Maria Silva", "maria@ex.com", "(47) 99999-1234", cpf, "1234567", cnh, null,
        "SC", "Rio do Sul", "Centro", "Rua XV", "10", null);

    private static DadosClienteDto PessoaJuridica(string? cnpj = "11.222.333/0001-81") => new(
        "Transportes Alfa", "alfa@ex.com", "(47) 3333-4444", null, null, null, cnpj,
        "SC", "Rio do Sul", "Centro", "Rua XV", "10", null);

    [Fact]
    public void Pessoa_fisica_valida_passa()
    {
        new PessoaFisicaDadosValidator().TestValidate(PessoaFisica()).ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Pessoa_fisica_com_cpf_de_digito_errado_e_rejeitada()
    {
        new PessoaFisicaDadosValidator().TestValidate(PessoaFisica(cpf: "111.444.777-36"))
            .ShouldHaveValidationErrorFor(c => c.Cpf);
    }

    [Fact]
    public void Pessoa_fisica_com_cnh_curta_e_rejeitada()
    {
        new PessoaFisicaDadosValidator().TestValidate(PessoaFisica(cnh: "123"))
            .ShouldHaveValidationErrorFor(c => c.Cnh);
    }

    [Fact]
    public void Pessoa_juridica_valida_passa()
    {
        new PessoaJuridicaDadosValidator().TestValidate(PessoaJuridica()).ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Pessoa_juridica_com_cnpj_invalido_e_rejeitada()
    {
        new PessoaJuridicaDadosValidator().TestValidate(PessoaJuridica("11.222.333/0001-82"))
            .ShouldHaveValidationErrorFor(c => c.Cnpj);
    }

    [Fact]
    public void Cliente_com_email_telefone_e_estado_invalidos_e_rejeitado()
    {
        var dados = PessoaJuridica() with { Email = "sem-arroba", Telefone = "123", Estado = "Santa Catarina" };

        var resultado = new PessoaJuridicaDadosValidator().TestValidate(dados);

        resultado.ShouldHaveValidationErrorFor(c => c.Email);
        resultado.ShouldHaveValidationErrorFor(c => c.Telefone);
        resultado.ShouldHaveValidationErrorFor(c => c.Estado);
    }

    // ---------------- Aluguel ----------------

    private static DadosAluguelDto Aluguel(
        DateOnly? saida = null, DateOnly? retorno = null, int km = 0, IReadOnlyList<Guid>? taxas = null)
    {
        var s = saida ?? new DateOnly(2026, 11, 1);

        return new DadosAluguelDto(
            Guid.NewGuid(), Guid.NewGuid(), TipoPlano.Diario,
            s, retorno ?? s.AddDays(3), km, TipoSeguro.Nenhum, taxas ?? []);
    }

    [Fact]
    public void Aluguel_valido_passa()
    {
        new DadosAluguelValidator().TestValidate(Aluguel()).ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Aluguel_no_mesmo_dia_e_valido()
    {
        var saida = new DateOnly(2026, 11, 1);

        new DadosAluguelValidator().TestValidate(Aluguel(saida, saida)).ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Aluguel_com_retorno_antes_da_saida_e_rejeitado()
    {
        var saida = new DateOnly(2026, 11, 10);

        new DadosAluguelValidator().TestValidate(Aluguel(saida, saida.AddDays(-1)))
            .ShouldHaveValidationErrorFor(a => a.DataRetornoPrevista);
    }

    [Fact]
    public void Aluguel_de_mais_de_365_dias_e_rejeitado()
    {
        var saida = new DateOnly(2026, 1, 1);

        var validador = new DadosAluguelValidator();

        validador.TestValidate(Aluguel(saida, saida.AddDays(365))).ShouldNotHaveAnyValidationErrors();
        validador.TestValidate(Aluguel(saida, saida.AddDays(366)))
            .ShouldHaveValidationErrorFor(a => a.DataRetornoPrevista);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(2_000_001)]
    public void Aluguel_com_km_inicial_fora_da_faixa_e_rejeitado(int km)
    {
        new DadosAluguelValidator().TestValidate(Aluguel(km: km)).ShouldHaveValidationErrorFor(a => a.KmInicial);
    }

    [Fact]
    public void Aluguel_com_ids_vazios_e_enums_invalidos_e_rejeitado()
    {
        var dados = Aluguel() with
        {
            CondutorId = Guid.Empty,
            AutomovelId = Guid.Empty,
            TipoPlano = (TipoPlano)99,
            TipoSeguro = (TipoSeguro)99
        };

        var resultado = new DadosAluguelValidator().TestValidate(dados);

        resultado.ShouldHaveValidationErrorFor(a => a.CondutorId);
        resultado.ShouldHaveValidationErrorFor(a => a.AutomovelId);
        resultado.ShouldHaveValidationErrorFor(a => a.TipoPlano);
        resultado.ShouldHaveValidationErrorFor(a => a.TipoSeguro);
    }

    [Fact]
    public void Devolucao_com_km_negativo_ou_sem_data_e_rejeitada()
    {
        var validador = new DadosDevolucaoValidator();

        validador.TestValidate(new DevolverAluguelCommand(Guid.NewGuid(), new DateOnly(2026, 11, 4), 1200, true))
            .ShouldNotHaveAnyValidationErrors();

        validador.TestValidate(new DevolverAluguelCommand(Guid.NewGuid(), default, -5, true))
            .ShouldHaveValidationErrorFor(c => c.KmFinal);

        validador.TestValidate(new DevolverAluguelCommand(Guid.NewGuid(), default, 10, true))
            .ShouldHaveValidationErrorFor(c => c.DataDevolucao);
    }
}
