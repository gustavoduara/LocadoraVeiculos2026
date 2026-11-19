namespace LocadoraDeVeiculos.Aplicacao.ModuloAutomovel;

/// <summary>
/// Identifica o tipo real de uma imagem pelos primeiros bytes (assinatura do arquivo),
/// sem confiar no content-type nem na extensão enviados pelo cliente.
/// SVG é propositalmente recusado (pode carregar scripts).
/// </summary>
public static class ImagemUtil
{
    public const int TamanhoMaximoEmBytes = 2 * 1024 * 1024;

    public static string? DetectarTipo(byte[] bytes)
    {
        if (bytes.Length >= 3 && bytes[0] == 0xFF && bytes[1] == 0xD8 && bytes[2] == 0xFF)
            return "image/jpeg";

        if (bytes.Length >= 8 &&
            bytes[0] == 0x89 && bytes[1] == 0x50 && bytes[2] == 0x4E && bytes[3] == 0x47 &&
            bytes[4] == 0x0D && bytes[5] == 0x0A && bytes[6] == 0x1A && bytes[7] == 0x0A)
            return "image/png";

        // WebP: "RIFF" + 4 bytes de tamanho + "WEBP"
        if (bytes.Length >= 12 &&
            bytes[0] == 0x52 && bytes[1] == 0x49 && bytes[2] == 0x46 && bytes[3] == 0x46 &&
            bytes[8] == 0x57 && bytes[9] == 0x45 && bytes[10] == 0x42 && bytes[11] == 0x50)
            return "image/webp";

        return null;
    }

    /// <summary>Remove caminho e caracteres de controle do nome enviado pelo cliente.</summary>
    public static string NomeSeguro(string? nome)
    {
        string apenasNome = Path.GetFileName(nome ?? string.Empty);

        string limpo = new(apenasNome.Where(c => !char.IsControl(c)).ToArray());

        if (string.IsNullOrWhiteSpace(limpo))
            return "foto";

        return limpo.Length > 255 ? limpo[..255] : limpo;
    }
}
