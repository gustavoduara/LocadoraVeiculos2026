const TIPOS_ACEITOS = ['image/jpeg', 'image/png', 'image/webp'];
const TAMANHO_MAXIMO_BYTES = 2 * 1024 * 1024;

/**
 * Prepara uma foto para envio: reduz para no máximo `ladoMaximo` pixels e converte para JPEG.
 * Fotos de celular têm vários MB; isso as deixa em algumas centenas de KB (o servidor aceita até 2 MB).
 * Lança um Error com mensagem amigável quando o arquivo não é uma imagem aceita.
 */
export async function comprimirImagem(
    arquivo: File,
    ladoMaximo = 1280,
    qualidade = 0.85,
): Promise<File> {
    if (!TIPOS_ACEITOS.includes(arquivo.type))
        throw new Error('Selecione uma imagem JPEG, PNG ou WebP.');

    let bitmap: ImageBitmap;

    try {
        bitmap = await createImageBitmap(arquivo);
    } catch {
        throw new Error('Não foi possível ler a imagem selecionada.');
    }

    const escala = Math.min(1, ladoMaximo / Math.max(bitmap.width, bitmap.height));
    const largura = Math.max(1, Math.round(bitmap.width * escala));
    const altura = Math.max(1, Math.round(bitmap.height * escala));

    const canvas = document.createElement('canvas');
    canvas.width = largura;
    canvas.height = altura;

    const contexto = canvas.getContext('2d');

    if (!contexto) throw new Error('Não foi possível processar a imagem selecionada.');

    // Fundo branco: evita que transparência de PNG/WebP vire preto no JPEG.
    contexto.fillStyle = '#ffffff';
    contexto.fillRect(0, 0, largura, altura);
    contexto.drawImage(bitmap, 0, 0, largura, altura);
    bitmap.close();

    const blob = await new Promise<Blob | null>((resolve) =>
        canvas.toBlob(resolve, 'image/jpeg', qualidade),
    );

    if (!blob) throw new Error('Não foi possível processar a imagem selecionada.');

    // Se a original já era menor e válida, mantém a original.
    if (arquivo.size <= blob.size && arquivo.size <= TAMANHO_MAXIMO_BYTES) return arquivo;

    if (blob.size > TAMANHO_MAXIMO_BYTES) throw new Error('A imagem é grande demais, mesmo reduzida.');

    const nomeBase = arquivo.name.replace(/\.[^.]+$/, '') || 'foto';

    return new File([blob], `${nomeBase}.jpg`, { type: 'image/jpeg' });
}
