namespace LocadoraDeVeiculos.Dominio.ModuloAutomovel;

/// <summary>
/// Foto extra de um carro (outros ângulos, para o carrossel). A foto principal continua no próprio <see cref="Automovel"/>
/// (é a capa); estas vêm depois, na ordem em que foram enviadas. Sempre se chega a elas pelo carro (que já filtra por empresa).
/// </summary>
public class AutomovelFoto
{
    /// <summary>Quantas fotos extras um carro pode ter (além da capa).</summary>
    public const int MaximoPorAutomovel = 5;

    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid AutomovelId { get; set; }
    public Automovel? Automovel { get; set; }

    public int Ordem { get; set; }
    public byte[] Conteudo { get; set; } = [];
    public string TipoConteudo { get; set; } = string.Empty;
    public string NomeArquivo { get; set; } = string.Empty;
    public DateTime CriadaEmUtc { get; set; } = DateTime.UtcNow;

    protected AutomovelFoto() { }

    public AutomovelFoto(Guid automovelId, int ordem, byte[] conteudo, string tipoConteudo, string nomeArquivo)
    {
        AutomovelId = automovelId;
        Ordem = ordem;
        Conteudo = conteudo;
        TipoConteudo = tipoConteudo;
        NomeArquivo = nomeArquivo;
    }
}
