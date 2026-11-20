namespace LocadoraDeVeiculos.Dominio.ModuloPerfilCliente;

/// <summary>
/// Dados que a pessoa com conta de cliente informa uma vez para poder reservar carros (documentos, contato e endereço).
/// Não pertence a nenhuma locadora: quando uma reserva é aprovada, a locadora recebe uma cópia destes dados.
/// </summary>
public class PerfilCliente
{
    /// <summary>A chave é o id da conta do cliente: cada conta tem no máximo um perfil.</summary>
    public Guid UsuarioId { get; set; }

    public string Cpf { get; set; } = string.Empty;
    public string Rg { get; set; } = string.Empty;
    public string Cnh { get; set; } = string.Empty;
    public DateOnly ValidadeCnh { get; set; }
    public string Telefone { get; set; } = string.Empty;

    public string Estado { get; set; } = string.Empty;
    public string Cidade { get; set; } = string.Empty;
    public string Bairro { get; set; } = string.Empty;
    public string Rua { get; set; } = string.Empty;
    public string Numero { get; set; } = string.Empty;

    public DateTimeOffset AtualizadoEmUtc { get; set; } = DateTimeOffset.UtcNow;

    public void Atualizar(PerfilCliente novo)
    {
        Cpf = novo.Cpf;
        Rg = novo.Rg;
        Cnh = novo.Cnh;
        ValidadeCnh = novo.ValidadeCnh;
        Telefone = novo.Telefone;
        Estado = novo.Estado;
        Cidade = novo.Cidade;
        Bairro = novo.Bairro;
        Rua = novo.Rua;
        Numero = novo.Numero;
        AtualizadoEmUtc = DateTimeOffset.UtcNow;
    }
}
