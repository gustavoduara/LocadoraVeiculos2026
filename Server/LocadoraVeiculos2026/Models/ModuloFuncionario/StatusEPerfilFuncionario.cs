namespace LocadoraDeVeiculos.WebApi.Models.ModuloFuncionario;

public record AlterarStatusFuncionarioRequest(bool Ativo);

public record AlterarStatusFuncionarioResponse(Guid Id, bool Ativo);

public record EditarMeuPerfilRequest(string NomeCompleto, string Cpf);

public record EditarMeuPerfilResponse(string NomeCompleto, string Cpf);

public record SelecionarMeuPerfilResponse(
    Guid Id,
    string NomeCompleto,
    string Cpf,
    string Email,
    decimal Salario,
    DateTimeOffset AdmissaoEmUtc
);
