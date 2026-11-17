/**
 * Extrai uma mensagem amigável de um HttpErrorResponse da API.
 * A API devolve um array de strings: o primeiro item é o título do erro
 * (ex.: "Requisição inválida") e os seguintes são os detalhes.
 */
export function mensagemDeErro(err: unknown, padrao: string): string {
    const corpo = (err as { error?: unknown })?.error;

    if (Array.isArray(corpo) && corpo.length > 0) {
        const textos = corpo.filter((m): m is string => typeof m === 'string' && m.length > 0);

        if (textos.length > 1) return textos.slice(1).join(' ');
        if (textos.length === 1) return textos[0];
    }

    if (typeof corpo === 'string' && corpo) return corpo;

    const mensagem = (corpo as { mensagem?: string } | null)?.mensagem;

    return mensagem || padrao;
}
