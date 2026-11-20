using Microsoft.AspNetCore.Identity;

namespace LocadoraDeVeiculos.Aplicacao.Compartilhado;

/// <summary>Traduz os erros do ASP.NET Identity (em inglês) para mensagens claras para o usuário.</summary>
public static class ErrosDeIdentity
{
    public static IEnumerable<string> Traduzir(IEnumerable<IdentityError> erros) =>
        erros.Select(erro => erro.Code switch
        {
            "DuplicateUserName" => "Já existe um usuário com esse e-mail.",
            "DuplicateEmail" => "Já existe um usuário com esse e-mail.",
            "InvalidEmail" => "O e-mail informado é inválido.",
            "InvalidUserName" => "O e-mail informado é inválido.",
            "PasswordTooShort" => "A senha é muito curta (mínimo de 6 caracteres).",
            "PasswordRequiresNonAlphanumeric" => "A senha deve conter pelo menos um caractere especial.",
            "PasswordRequiresDigit" => "A senha deve conter pelo menos um número.",
            "PasswordRequiresUpper" => "A senha deve conter pelo menos uma letra maiúscula.",
            "PasswordRequiresLower" => "A senha deve conter pelo menos uma letra minúscula.",
            "PasswordRequiresUniqueChars" => "A senha deve conter mais caracteres diferentes.",
            _ => erro.Description
        });
}
