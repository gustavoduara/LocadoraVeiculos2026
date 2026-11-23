using LocadoraDeVeiculos.Dominio.Compartilhado;
using LocadoraDeVeiculos.Dominio.ModuloGrupoVeiculos;

namespace LocadoraDeVeiculos.Dominio.ModuloAutomovel;

/// <summary>Veículo da frota da locadora. Pertence a um grupo de veículos e pode ter uma foto.</summary>
public class Automovel : EntidadeBase<Automovel>
{
    /// <summary>Placa normalizada: maiúscula e sem hífen (ex.: ABC1D23).</summary>
    public string Placa { get; set; } = string.Empty;
    public string Marca { get; set; } = string.Empty;
    public string Modelo { get; set; } = string.Empty;
    public string Cor { get; set; } = string.Empty;
    public int Ano { get; set; }
    public TipoCombustivel TipoCombustivel { get; set; }
    public int CapacidadeCombustivelLitros { get; set; }

    /// <summary>Valor por dia do seguro deste carro, escolhido pela locadora. Zero = o carro não oferece seguro.</summary>
    public decimal SeguroDiaria { get; set; }

    public Guid GrupoVeiculosId { get; set; }
    public GrupoVeiculos? GrupoVeiculos { get; set; }

    public byte[]? Foto { get; set; }
    public string? FotoTipoConteudo { get; set; }
    public string? FotoNomeArquivo { get; set; }

    public bool TemFoto => Foto is { Length: > 0 };

    /// <summary>Fotos extras do carrossel (a foto principal fica nos campos acima).</summary>
    public List<AutomovelFoto> FotosExtras { get; set; } = [];

    protected Automovel() { }

    public Automovel(
        Guid empresaId,
        string placa,
        string marca,
        string modelo,
        string cor,
        int ano,
        TipoCombustivel tipoCombustivel,
        int capacidadeCombustivelLitros,
        Guid grupoVeiculosId,
        decimal seguroDiaria = 0m
    )
    {
        EmpresaId = empresaId;
        Placa = placa;
        Marca = marca;
        Modelo = modelo;
        Cor = cor;
        Ano = ano;
        TipoCombustivel = tipoCombustivel;
        CapacidadeCombustivelLitros = capacidadeCombustivelLitros;
        GrupoVeiculosId = grupoVeiculosId;
        SeguroDiaria = seguroDiaria;
    }

    public void AtualizarFoto(byte[] conteudo, string tipoConteudo, string nomeArquivo)
    {
        Foto = conteudo;
        FotoTipoConteudo = tipoConteudo;
        FotoNomeArquivo = nomeArquivo;
    }

    public void RemoverFoto()
    {
        Foto = null;
        FotoTipoConteudo = null;
        FotoNomeArquivo = null;
    }

    /// <summary>Atualiza os dados cadastrais (a foto tem operações próprias).</summary>
    public override void AtualizarRegistro(Automovel registroEditado)
    {
        Placa = registroEditado.Placa;
        Marca = registroEditado.Marca;
        Modelo = registroEditado.Modelo;
        Cor = registroEditado.Cor;
        Ano = registroEditado.Ano;
        TipoCombustivel = registroEditado.TipoCombustivel;
        CapacidadeCombustivelLitros = registroEditado.CapacidadeCombustivelLitros;
        GrupoVeiculosId = registroEditado.GrupoVeiculosId;
        SeguroDiaria = registroEditado.SeguroDiaria;
    }
}
