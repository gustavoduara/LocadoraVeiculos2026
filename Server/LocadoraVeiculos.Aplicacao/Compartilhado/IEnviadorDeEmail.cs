namespace LocadoraDeVeiculos.Aplicacao.Compartilhado;

/// <summary>Envio de e-mails do sistema (redefinição de senha). A implementação é escolhida na configuração.</summary>
public interface IEnviadorDeEmail
{
    Task EnviarAsync(string para, string assunto, string corpoTexto, CancellationToken cancellationToken = default);
}
