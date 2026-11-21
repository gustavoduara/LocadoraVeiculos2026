using FluentResults;

namespace LocadoraDeVeiculos.Aplicacao.Compartilhado;

public abstract class ResultadosErro
{
    public static Error RequisicaoInvalidaErro(string erro)
    {
        return new Error("Requisição inválida")
            .CausedBy(erro)
            .WithMetadata("TipoErro", "RequisicaoInvalida");
    }

    public static Error RequisicaoInvalidaErro(IEnumerable<string> erros)
    {
        return new Error("Requisição inválida")
            .CausedBy(erros)
            .WithMetadata("TipoErro", "RequisicaoInvalida");
    }

    public static Error RegistroDuplicadoErro(string mensagemErro)
    {
        return new Error("Registro duplicado")
            .CausedBy(mensagemErro)
            .WithMetadata("TipoErro", "RegistroDuplicado");
    }

    public static Error RegistroNaoEncontradoErro(Guid id)
    {
        return new Error("Registro não encontrado")
            .CausedBy("Não foi possível obter o registro com o ID: " + id)
            .WithMetadata("TipoErro", "RegistroNaoEncontrado");
    }

    public static Error RegistroNaoEncontradoErro(string registro)
    {
        return new Error("Registro não encontrado")
            .CausedBy("Não foi possível obter o registro: " + registro)
            .WithMetadata("TipoErro", "RegistroNaoEncontrado");
    }


    public static Error ExclusaoBloqueadaErro(string mensagemErro)
    {
        return new Error("Exclusão bloqueada")
            .CausedBy(mensagemErro)
            .WithMetadata("TipoErro", "ExclusaoBloqueada");
    }


    /// <summary>Edição ou outra operação (que não seja exclusão) barrada por uma regra de negócio.</summary>
    public static Error OperacaoBloqueadaErro(string mensagemErro)
    {
        return new Error("Operação bloqueada")
            .CausedBy(mensagemErro)
            .WithMetadata("TipoErro", "OperacaoBloqueada");
    }

    /// <summary>Sessão ou credencial inválida, vencida ou revogada (vira 401, não 500).</summary>
    public static Error NaoAutorizadoErro(string mensagemErro)
    {
        return new Error("Não autorizado")
            .CausedBy(mensagemErro)
            .WithMetadata("TipoErro", "NaoAutorizado");
    }

    /// <summary>A senha está certa, mas o e-mail da conta ainda não foi confirmado (vira 403).</summary>
    public static Error EmailNaoConfirmadoErro()
    {
        return new Error("E-mail não confirmado")
            .CausedBy("Confirme o seu e-mail para entrar. Enviamos um link para o endereço cadastrado.")
            .WithMetadata("TipoErro", "EmailNaoConfirmado");
    }

    public static Error ExcecaoInternaErro(Exception ex)
    {
        return new Error("Ocorreu um erro interno do servidor")
            .CausedBy(ex)
            .WithMetadata("TipoErro", "ExcecaoInterna");
    }
}