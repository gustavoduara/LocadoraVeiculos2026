using LocadoraDeVeiculos.Dominio.ModuloAutenticacao;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace LocadoraDeVeiculos.WebApi.Config.Identity;

/// <summary>
/// O link de confirmação de e-mail vale por 24 horas (o de "esqueci minha senha" continua valendo 1 hora):
/// a pessoa pode abrir o e-mail mais tarde, sem que o link de senha fique frouxo.
/// </summary>
public class ConfirmacaoDeEmailTokenProviderOptions : DataProtectionTokenProviderOptions
{
    public const string NomeDoProvedor = "ConfirmacaoDeEmail";

    public ConfirmacaoDeEmailTokenProviderOptions()
    {
        Name = NomeDoProvedor;
        TokenLifespan = TimeSpan.FromHours(24);
    }
}

public class ConfirmacaoDeEmailTokenProvider(
    IDataProtectionProvider protecao,
    IOptions<ConfirmacaoDeEmailTokenProviderOptions> opcoes,
    ILogger<DataProtectorTokenProvider<Usuario>> logger
) : DataProtectorTokenProvider<Usuario>(protecao, opcoes, logger);
